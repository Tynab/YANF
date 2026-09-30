using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Enum;

namespace YANF.Script
{
    /// <summary>
    /// Shows a Load, Wait or Update screen over a form while work runs, on the form's own UI thread.
    /// </summary>
    /// <remarks>
    /// <para>The screen is an owned, non-modal window: it stays above its owner (and only above it), follows it when it is moved or
    /// resized (an MDI child or embedded form also when its parents move), and hides while it is minimized. The owner takes no mouse
    /// or keyboard input from the start, also while the screen waits for its delay, like the owner of a modal dialog; its controls are
    /// not greyed out. When several loaders are open on one form, it gets its input back when the last one closes. The screen appears
    /// only when the work is still running after <see cref="YANLoaderOptions.ShowDelay"/>, so short work shows nothing.</para>
    /// <para>Use <see cref="RunWithLoaderAsync(Form, Func{IProgress{int}, CancellationToken, Task})"/> from an async event handler, with the
    /// work on the thread pool; or <see cref="Show(Form)"/> in a <c>using</c> block around awaited work. The legacy services in
    /// <c>YANF.Script.Service</c> remain for code that blocks the UI thread (they run the screen on a thread of its own).</para>
    /// <para>The progress object that RunWithLoaderAsync passes to the work is the <see cref="YANLoaderScope"/> of the screen: cast it to
    /// call <see cref="YANLoaderScope.SetProgress"/>, which also shows a detail text on the Update screen.</para>
    /// <para>A message box or dialog that the work opens before the screen has appeared is never covered by it (see
    /// <see cref="YANLoaderScope"/>).</para>
    /// <code>
    /// // C#
    /// private async void BtnLoad_Click(object sender, EventArgs e)
    /// {
    ///     var rows = await this.RunWithLoaderAsync((progress, ct) => Task.Run(() => _repository.Load(progress, ct), ct));
    ///     grid.DataSource = rows;
    /// }
    ///
    /// private async void BtnDownload_Click(object sender, EventArgs e)
    /// {
    ///     var options = new YANLoaderOptions { Kind = YANLoaderKind.Update };
    ///     await this.RunWithLoaderAsync((progress, ct) => Task.Run(() => _updater.Download((done, total) =>
    ///         ((YANLoaderScope)progress).SetProgress((int)(done * 100 / total), $"{done >> 20} MB / {total >> 20} MB"), ct), ct), options, CancellationToken.None);
    /// }
    ///
    /// // or, around awaited work
    /// using (var scope = YANLoader.Show(this, options))
    /// {
    ///     await _updater.DownloadAsync((done, total) => scope.SetProgress((int)(done * 100 / total), $"{done >> 20} MB / {total >> 20} MB"));
    /// }
    /// </code>
    /// <code>
    /// ' VB
    /// Private Async Sub BtnDownload_Click(sender As Object, e As EventArgs) Handles BtnDownload.Click
    ///     Dim options As New YANLoaderOptions With {.Kind = YANLoaderKind.Update}
    ///     Await Me.RunWithLoaderAsync(
    ///         Function(progress, ct) Task.Run(
    ///             Sub() _updater.Download(
    ///                 Sub(done, total) DirectCast(progress, YANLoaderScope).SetProgress(CInt(done * 100 \ total), $"{done >> 20} MB / {total >> 20} MB"), ct), ct),
    ///         options, CancellationToken.None)
    /// End Sub
    /// </code>
    /// </remarks>
    public static class YANLoader
    {
        #region Methods
        /// <summary>
        /// Shows a Load screen over <paramref name="owner"/> (after the default delay) and blocks its input until the returned scope is disposed.
        /// </summary>
        /// <param name="owner">Form to cover. Call this on its UI thread.</param>
        /// <returns>The scope: report progress through it, and dispose it on the UI thread when the work is done.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> is null.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="owner"/> is disposed.</exception>
        /// <exception cref="InvalidOperationException">Called on another thread than the owner's UI thread.</exception>
        public static YANLoaderScope Show(Form owner) => Show(owner, null);

        /// <summary>
        /// Shows a screen over <paramref name="owner"/> as set in <paramref name="options"/> and blocks its input until the returned scope is disposed.
        /// </summary>
        /// <param name="owner">Form to cover. Call this on its UI thread.</param>
        /// <param name="options">Screen kind, show delay, corner and fade duration; null uses the defaults.</param>
        /// <returns>The scope: report progress through it, and dispose it on the UI thread when the work is done.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="YANLoaderOptions.Kind"/> is not a <see cref="YANLoaderKind"/> value.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="owner"/> is disposed.</exception>
        /// <exception cref="InvalidOperationException">Called on another thread than the owner's UI thread.</exception>
        public static YANLoaderScope Show(Form owner, YANLoaderOptions options)
        {
            CheckOwner(owner);
            options ??= new YANLoaderOptions();
            if (!IsDefined(typeof(YANLoaderKind), options.Kind))
            {
                throw new ArgumentOutOfRangeException(nameof(options), options.Kind, "Unknown YANLoaderKind.");
            }
            return new YANLoaderScope(owner, options);
        }

        /// <summary>
        /// Runs <paramref name="work"/> with a Load screen over <paramref name="owner"/> (shown after the default delay) and blocks the owner's input until it ends.
        /// </summary>
        /// <param name="owner">Form to cover. Call this on its UI thread.</param>
        /// <param name="work">Starts the work and returns its task. It is called on the UI thread: put long work on the thread pool
        /// (<see cref="Task.Run(Func{Task})"/>). The progress it receives can be used from any thread;
        /// it is the <see cref="YANLoaderScope"/> of the screen, which can be cast to call <see cref="YANLoaderScope.SetProgress"/> (a detail text).</param>
        /// <returns>A task that ends when the work has ended and the screen is gone (the owner takes no input until then), with the work's
        /// exception or cancellation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> or <paramref name="work"/> is null.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="owner"/> is disposed.</exception>
        /// <exception cref="InvalidOperationException">Called on another thread than the owner's UI thread.</exception>
        public static Task RunWithLoaderAsync(this Form owner, Func<IProgress<int>, CancellationToken, Task> work) => RunWithLoaderAsync(owner, work, null, CancellationToken.None);

        /// <summary>
        /// Runs <paramref name="work"/> with a screen over <paramref name="owner"/> as set in <paramref name="options"/> and blocks the owner's input until it ends.
        /// </summary>
        /// <param name="owner">Form to cover. Call this on its UI thread.</param>
        /// <param name="work">Starts the work and returns its task. It is called on the UI thread: put long work on the thread pool
        /// (<see cref="Task.Run(Func{Task})"/>). The progress it receives can be used from any thread;
        /// it is the <see cref="YANLoaderScope"/> of the screen, which can be cast to call <see cref="YANLoaderScope.SetProgress"/> (a detail text).</param>
        /// <param name="options">Screen kind, show delay, corner and fade duration; null uses the defaults.</param>
        /// <param name="cancellationToken">Passed to <paramref name="work"/>. When it is already cancelled, nothing is shown and the returned task is cancelled.</param>
        /// <returns>A task that ends when the work has ended and the screen is gone (the owner takes no input until then), with the work's
        /// exception or cancellation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> or <paramref name="work"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="YANLoaderOptions.Kind"/> is not a <see cref="YANLoaderKind"/> value.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="owner"/> is disposed.</exception>
        /// <exception cref="InvalidOperationException">Called on another thread than the owner's UI thread.</exception>
        public static Task RunWithLoaderAsync(this Form owner, Func<IProgress<int>, CancellationToken, Task> work, YANLoaderOptions options, CancellationToken cancellationToken)
        {
            CheckOwner(owner);
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : RunAsync(Show(owner, options), work, cancellationToken);
        }

        /// <summary>
        /// Runs <paramref name="work"/> with a Load screen over <paramref name="owner"/> (shown after the default delay), blocks the owner's
        /// input until it ends and returns its result.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="owner">Form to cover. Call this on its UI thread.</param>
        /// <param name="work">Starts the work and returns its task. It is called on the UI thread: put long work on the thread pool
        /// (<see cref="Task.Run{TResult}(Func{TResult})"/>). The progress it receives can be used from any thread;
        /// it is the <see cref="YANLoaderScope"/> of the screen, which can be cast to call <see cref="YANLoaderScope.SetProgress"/> (a detail text).</param>
        /// <returns>A task with the work's result, exception or cancellation, which ends when the screen is gone (the owner takes no input
        /// until then).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> or <paramref name="work"/> is null.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="owner"/> is disposed.</exception>
        /// <exception cref="InvalidOperationException">Called on another thread than the owner's UI thread.</exception>
        public static Task<T> RunWithLoaderAsync<T>(this Form owner, Func<IProgress<int>, CancellationToken, Task<T>> work) => RunWithLoaderAsync<T>(owner, work, null, CancellationToken.None);

        /// <summary>
        /// Runs <paramref name="work"/> with a screen over <paramref name="owner"/> as set in <paramref name="options"/>, blocks the owner's
        /// input until it ends and returns its result.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="owner">Form to cover. Call this on its UI thread.</param>
        /// <param name="work">Starts the work and returns its task. It is called on the UI thread: put long work on the thread pool
        /// (<see cref="Task.Run{TResult}(Func{TResult})"/>). The progress it receives can be used from any thread;
        /// it is the <see cref="YANLoaderScope"/> of the screen, which can be cast to call <see cref="YANLoaderScope.SetProgress"/> (a detail text).</param>
        /// <param name="options">Screen kind, show delay, corner and fade duration; null uses the defaults.</param>
        /// <param name="cancellationToken">Passed to <paramref name="work"/>. When it is already cancelled, nothing is shown and the returned task is cancelled.</param>
        /// <returns>A task with the work's result, exception or cancellation, which ends when the screen is gone (the owner takes no input
        /// until then).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="owner"/> or <paramref name="work"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="YANLoaderOptions.Kind"/> is not a <see cref="YANLoaderKind"/> value.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="owner"/> is disposed.</exception>
        /// <exception cref="InvalidOperationException">Called on another thread than the owner's UI thread.</exception>
        public static Task<T> RunWithLoaderAsync<T>(this Form owner, Func<IProgress<int>, CancellationToken, Task<T>> work, YANLoaderOptions options, CancellationToken cancellationToken)
        {
            CheckOwner(owner);
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled<T>(cancellationToken) : RunAsync<T>(Show(owner, options), work, cancellationToken);
        }

        // Owner usable from this thread
        private static void CheckOwner(Form owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
            if (owner.IsDisposed)
            {
                throw new ObjectDisposedException(owner.GetType().FullName, "The owner form is disposed.");
            }
            if (owner.InvokeRequired)
            {
                throw new InvalidOperationException("YANLoader must be used on the UI thread of the owner form.");
            }
        }

        // UI thread: start the work, wait for it (continuing on the UI thread), always close the scope
        private static async Task RunAsync(YANLoaderScope scope, Func<IProgress<int>, CancellationToken, Task> work, CancellationToken cancellationToken)
        {
            try
            {
                await (work(scope, cancellationToken) ?? throw NoTask());
            }
            finally
            {
                await scope.CloseAsync();
            }
        }

        // UI thread: start the work, wait for its result (continuing on the UI thread), always close the scope
        private static async Task<T> RunAsync<T>(YANLoaderScope scope, Func<IProgress<int>, CancellationToken, Task<T>> work, CancellationToken cancellationToken)
        {
            try
            {
                return await (work(scope, cancellationToken) ?? throw NoTask());
            }
            finally
            {
                await scope.CloseAsync();
            }
        }

        // The work delegate returned no task
        private static InvalidOperationException NoTask() => new("The work delegate returned null instead of a task.");
        #endregion
    }
}
