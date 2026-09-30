using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YANF.Screen;
using static System.Math;
using static YANF.Script.YANLoaderKind;
using Timer = System.Windows.Forms.Timer;
using WinControl = System.Windows.Forms.Control;

namespace YANF.Script
{
    /// <summary>
    /// A Load, Wait or Update screen shown over a form by <see cref="YANLoader.Show(Form, YANLoaderOptions)"/>.
    /// While the scope is open the owner form takes no mouse or keyboard input; dispose the scope (a <c>using</c> block)
    /// to close the screen and give the owner its input back.
    /// </summary>
    /// <remarks>
    /// <para>Create and dispose the scope on the owner form's UI thread. <see cref="Report"/> and <see cref="SetProgress"/> can be called
    /// from any thread: the values are shown on the UI thread, and when they arrive faster than it can show them only the latest is shown.</para>
    /// <para>A window opened while the screen waits for its delay (a message box or dialog shown by the work) is never covered: the
    /// screen appears only after that window is closed and the delay has elapsed once more.</para>
    /// </remarks>
    public sealed class YANLoaderScope : IDisposable, IProgress<int>
    {
        #region Fields
        private readonly object _sync = new();
        private readonly Form _owner;
        private readonly Thread _thread;
        private readonly SynchronizationContext _context;
        private readonly YANOverlayScreen _scr;
        private HashSet<IntPtr> _windowsBefore;
        private Timer _timer;
        private IntPtr _hwnd;
        private IntPtr _focus;
        private bool _is_OwnerBlocked;
        private bool _is_OwnerGone;
        private bool _is_DialogSeen;
        private Task _closing;
        // Shared with the reporting threads (under _sync)
        private int _percent;
        private string _detail;
        private bool _is_Closed;
        private bool _is_UpdateQueued;
        #endregion

        #region Constructors
        // On the owner's UI thread (checked by YANLoader): block the owner and show the screen now or after the delay
        internal YANLoaderScope(Form owner, YANLoaderOptions options)
        {
            _owner = owner;
            _thread = Thread.CurrentThread;
            // Like Progress<T>: post through the UI thread's context (none, or the thread-pool one: through the owner's window)
            var context = SynchronizationContext.Current;
            _context = context == null || context.GetType() == typeof(SynchronizationContext) ? null : context;
            _hwnd = owner.Handle;
            // Built before the owner is touched: a failure here leaves nothing to undo
            _scr = options.Kind switch
            {
                Wait => new YANWaitScreen(),
                Update => new YANUpdateScreen(),
                _ => new YANLoadScreen()
            };
            try
            {
                // An owned window stays above its owner only, never above other applications
                _scr.TopMost = false;
                _scr.FadeDuration = options.FadeDuration;
                _scr.Corner = options.Corner;
                _scr.Track(owner);
                owner.HandleCreated += Owner_HandleCreated;
                owner.HandleDestroyed += Owner_HandleDestroyed;
                BlockOwner();
                if (options.ShowDelay > 0)
                {
                    // Windows on screen now are never taken for a dialog of the work
                    _windowsBefore = VisibleWindows();
                    _timer = new Timer
                    {
                        Interval = options.ShowDelay
                    };
                    _timer.Tick += Timer_Tick;
                    _timer.Start();
                }
                else
                {
                    ShowScreen();
                }
            }
            catch
            {
                Stop();
                Release();
                _scr.Dispose();
                throw;
            }
        }
        #endregion

        #region Properties
        // user32 input calls; a test double replaces them where user32 does not exist (mono)
        internal static IWindowInput Native { get; set; } = new User32Input();

        // The screen of this scope (shown after the delay, disposed when the scope closes)
        internal YANOverlayScreen Screen => _scr;

        // This scope has disabled the owner window and has not given it back yet
        internal bool IsOwnerBlocked => _is_OwnerBlocked;
        #endregion

        #region Methods
        /// <summary>
        /// Shows <paramref name="value"/> percent on the screen, keeping the current detail text. Safe from any thread.
        /// </summary>
        /// <param name="value">Progress in percent; values outside 0 to 100 are clamped.</param>
        public void Report(int value) => Publish(value, null, false);

        /// <summary>
        /// Shows a percentage and a detail text on the screen (the Update screen shows the detail; the Wait screen shows no progress).
        /// Safe from any thread.
        /// </summary>
        /// <param name="percent">Progress in percent; values outside 0 to 100 are clamped.</param>
        /// <param name="detail">Detail text, for example "68 MB / 137 MB"; null shows none.</param>
        public void SetProgress(int percent, string detail) => Publish(percent, detail, true);

        /// <summary>
        /// Gives the owner form its input back at once, then closes the screen (with a fade-out when it is on screen). Calling it again does nothing.
        /// </summary>
        /// <exception cref="InvalidOperationException">Called on another thread than the owner form's UI thread. The scope is still
        /// closed on the UI thread, so the owner is never left without input.</exception>
        public void Dispose()
        {
            if (Thread.CurrentThread == _thread)
            {
                Observe(CloseOnUi(false));
                return;
            }
            _ = TryPost(_ => Observe(CloseOnUi(false)));
            throw new InvalidOperationException("A YANLoaderScope must be disposed on the UI thread of its owner form.");
        }

        // Close from any thread; the task ends when the screen is gone. Used by RunWithLoaderAsync: the owner stays blocked while the
        // screen fades out, so it takes no input before the code awaiting the work runs
        internal Task CloseAsync()
        {
            if (Thread.CurrentThread == _thread)
            {
                return CloseOnUi(true);
            }
            var closing = new TaskCompletionSource<Task>();
            if (!TryPost(_ =>
            {
                try
                {
                    _ = closing.TrySetResult(CloseOnUi(true));
                }
                catch (Exception ex)
                {
                    _ = closing.TrySetException(ex);
                }
            }))
            {
                // The UI thread is gone, and its windows with it
                _ = closing.TrySetResult(Task.CompletedTask);
            }
            return closing.Task.Unwrap();
        }

        // A failed close is reported like any other UI-thread failure (Application.ThreadException)
        private static async void Observe(Task task) => await task;

        // UI thread: close the screen once. The owner is given back before the screen closes (so Windows activates it when the screen
        // goes): at once (Dispose, or no screen on show), or right after the fade-out when isFadeBlocking (RunWithLoaderAsync)
        private Task CloseOnUi(bool isFadeBlocking)
        {
            if (_closing != null)
            {
                if (!isFadeBlocking)
                {
                    // Dispose while RunWithLoaderAsync closes: the owner is given back at once all the same
                    Release();
                }
                return _closing;
            }
            var isActive = !_scr.IsDisposed && _scr.Visible && Form.ActiveForm == _scr;
            _closing = Task.CompletedTask;
            Stop();
            if (!isFadeBlocking || _scr.IsDisposed || !_scr.IsHandleCreated)
            {
                Release();
            }
            if (_scr.IsDisposed)
            {
                return _closing;
            }
            if (!_scr.IsHandleCreated)
            {
                // Never shown (the work ended within the delay)
                _scr.Dispose();
                return _closing;
            }
            return _closing = CloseScreenAsync(isActive);
        }

        // UI thread: fade the screen out, give the owner back (if not done yet) and close the screen; bring the owner forward if the
        // screen was the active window
        private async Task CloseScreenAsync(bool isActive)
        {
            try
            {
                await _scr.CloseAnimatedAsync(Release);
            }
            finally
            {
                // Also when the close failed before it gave the owner back
                Release();
                if (isActive && !_is_OwnerGone && !_owner.IsDisposed && _owner.Visible && Form.ActiveForm == null)
                {
                    _owner.Activate();
                }
            }
        }

        // UI thread: drop later reports and stop the delay timer
        private void Stop()
        {
            lock (_sync)
            {
                _is_Closed = true;
            }
            StopTimer();
        }

        // UI thread: stop following the owner's window and unblock the owner (again: nothing)
        private void Release()
        {
            _owner.HandleCreated -= Owner_HandleCreated;
            _owner.HandleDestroyed -= Owner_HandleDestroyed;
            UnblockOwner();
        }

        // Any thread: keep the latest value, show it on the UI thread (at most one update waits there)
        private void Publish(int percent, string detail, bool hasDetail)
        {
            var isUiThread = Thread.CurrentThread == _thread;
            lock (_sync)
            {
                if (_is_Closed)
                {
                    return;
                }
                _percent = Max(0, Min(100, percent));
                if (hasDetail)
                {
                    _detail = detail;
                }
                if (!isUiThread)
                {
                    if (_is_UpdateQueued)
                    {
                        return;
                    }
                    _is_UpdateQueued = true;
                }
            }
            if (isUiThread)
            {
                ShowProgress();
            }
            else if (!TryPost(ShowQueuedProgress))
            {
                lock (_sync)
                {
                    _is_UpdateQueued = false;
                }
            }
        }

        // UI thread: a queued update
        private void ShowQueuedProgress(object state)
        {
            lock (_sync)
            {
                _is_UpdateQueued = false;
            }
            ShowProgress();
        }

        // UI thread: show the latest value
        private void ShowProgress()
        {
            int percent;
            string detail;
            lock (_sync)
            {
                if (_is_Closed)
                {
                    return;
                }
                percent = _percent;
                detail = _detail;
            }
            if (!_scr.IsDisposed)
            {
                _scr.SetProgress(percent, detail);
            }
        }

        // Post to the owner's UI thread; false when that is no longer possible (its windows are gone)
        private bool TryPost(SendOrPostCallback callback)
        {
            try
            {
                if (_context != null)
                {
                    _context.Post(callback, null);
                }
                else
                {
                    _ = _owner.BeginInvoke(callback, new object[] { null });
                }
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        // Delay elapsed with the work still running: show the screen, unless it would cover a dialog opened meanwhile. The timer then
        // keeps ticking: the screen appears one full delay after the last tick that saw a dialog
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (IsDialogOpen())
            {
                _is_DialogSeen = true;
            }
            else if (_is_DialogSeen)
            {
                // The dialog is gone: the work may end right after it, so wait one more delay
                _is_DialogSeen = false;
            }
            else
            {
                ShowScreen();
            }
        }

        // UI thread: a window opened since the scope started is in front of the owner and takes input (a message box or dialog shown
        // by the work). The screen, shown now, would hide it and take its keyboard focus while the work waits for its answer
        private bool IsDialogOpen()
        {
            // WinForms dialogs (ShowDialog), whatever their owner
            if (ThreadForms().Any(f => f.Modal && f.Visible && f is not YANOverlayScreen && !_windowsBefore.Contains(f.Handle)))
            {
                return true;
            }
            // Other windows (message boxes, common dialogs, owned forms): enabled, owned by the owner (directly or through another
            // window) or active. Tool windows (tooltips, drop-downs) and loader screens never count
            var scr = _scr.IsHandleCreated ? _scr.Handle : IntPtr.Zero;
            var active = Native.GetActiveWindow();
            return Native.GetThreadWindows().Any(h => h != _hwnd && h != scr && !_windowsBefore.Contains(h)
                && Native.IsWindowVisible(h) && Native.IsWindowEnabled(h) && !Native.IsToolWindow(h)
                && (h == active || IsOwnedByOwner(h)) && WinControl.FromHandle(h) is not YANOverlayScreen);
        }

        // The window is owned by the owner, directly or through the windows that own it
        private bool IsOwnedByOwner(IntPtr hwnd)
        {
            // Owner chains are short; the bound only guards against an endless loop
            for (var i = 0; i < 64; i++)
            {
                hwnd = Native.GetOwner(hwnd);
                if (hwnd == IntPtr.Zero)
                {
                    return false;
                }
                if (hwnd == _hwnd)
                {
                    return true;
                }
            }
            return false;
        }

        // UI thread: the visible top-level windows of this thread (user32) and its visible forms
        private static HashSet<IntPtr> VisibleWindows()
        {
            var windows = new HashSet<IntPtr>(Native.GetThreadWindows().Where(Native.IsWindowVisible));
            windows.UnionWith(ThreadForms().Where(f => f.Visible).Select(f => f.Handle));
            return windows;
        }

        // UI thread: the open forms of this thread with a window (Application.OpenForms holds the forms of every thread)
        private static List<Form> ThreadForms()
        {
            var openForms = Application.OpenForms;
            var forms = new List<Form>();
            for (var i = openForms.Count - 1; i >= 0; i--)
            {
                Form frm;
                try
                {
                    frm = openForms[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    // A form of another thread closed meanwhile
                    continue;
                }
                if (frm != null && frm.IsHandleCreated && !frm.InvokeRequired)
                {
                    forms.Add(frm);
                }
            }
            return forms;
        }

        // UI thread: show the screen over the owner (a failure goes to Application.ThreadException; the owner stays blocked until the scope closes)
        private void ShowScreen()
        {
            StopTimer();
            if (_closing != null || _is_OwnerGone || _scr.IsDisposed)
            {
                return;
            }
            try
            {
                _scr.ShowOverOwner();
            }
            catch
            {
                _scr.Dispose();
                throw;
            }
        }

        // Stop and release the delay timer
        private void StopTimer()
        {
            var timer = _timer;
            if (timer == null)
            {
                return;
            }
            _timer = null;
            timer.Tick -= Timer_Tick;
            timer.Dispose();
        }

        // Disable the owner window like a modal dialog does (Control.Enabled would grey out every control). An owner that is disabled
        // already (a modal dialog, an outer scope) is left to whoever disabled it
        private void BlockOwner()
        {
            if (!Native.IsWindowEnabled(_hwnd))
            {
                return;
            }
            var focus = Native.GetFocus();
            _focus = focus != IntPtr.Zero && (focus == _hwnd || Native.IsChild(_hwnd, focus)) ? focus : IntPtr.Zero;
            _ = Native.EnableWindow(_hwnd, false);
            _is_OwnerBlocked = true;
        }

        // Enable the owner window again, and give the keyboard focus back when the owner is still the active window
        private void UnblockOwner()
        {
            if (!_is_OwnerBlocked)
            {
                return;
            }
            _is_OwnerBlocked = false;
            if (_is_OwnerGone || _owner.IsDisposed || !_owner.IsHandleCreated)
            {
                return;
            }
            _ = Native.EnableWindow(_hwnd, true);
            if (_focus != IntPtr.Zero && Native.GetFocus() == IntPtr.Zero && Form.ActiveForm == _owner && (_focus == _hwnd || Native.IsChild(_hwnd, _focus)))
            {
                Native.SetFocus(_focus);
            }
        }

        // The owner window was recreated (RecreateHandle): the new window starts enabled, so block it again
        private void Owner_HandleCreated(object sender, EventArgs e)
        {
            _hwnd = _owner.Handle;
            _focus = IntPtr.Zero;
            if (_is_OwnerBlocked)
            {
                _ = Native.EnableWindow(_hwnd, false);
            }
        }

        // The owner closed or was disposed while the scope is open: its window and the owned screen are gone
        private void Owner_HandleDestroyed(object sender, EventArgs e)
        {
            if (_owner.RecreatingHandle)
            {
                return;
            }
            _is_OwnerGone = true;
            _is_OwnerBlocked = false;
            StopTimer();
            if (!_scr.IsDisposed)
            {
                _scr.Dispose();
            }
        }
        #endregion

        #region Nested types
        /// <summary>
        /// The user32 calls that block and unblock the owner window and find the dialogs in front of it.
        /// </summary>
        internal interface IWindowInput
        {
            bool IsWindowEnabled(IntPtr hWnd);

            bool EnableWindow(IntPtr hWnd, bool enable);

            IntPtr GetFocus();

            void SetFocus(IntPtr hWnd);

            bool IsChild(IntPtr hWndParent, IntPtr hWnd);

            // Top-level windows of the calling thread (EnumThreadWindows)
            IntPtr[] GetThreadWindows();

            bool IsWindowVisible(IntPtr hWnd);

            // GetWindow(GW_OWNER)
            IntPtr GetOwner(IntPtr hWnd);

            // WS_EX_TOOLWINDOW
            bool IsToolWindow(IntPtr hWnd);

            // Active window of the calling thread
            IntPtr GetActiveWindow();
        }

        /// <summary>
        /// The real user32 implementation.
        /// </summary>
        private sealed class User32Input : IWindowInput
        {
            private const uint GW_OWNER = 4;
            private const int GWL_EXSTYLE = -20;
            private const int WS_EX_TOOLWINDOW = 0x80;

            private delegate bool EnumThreadWndProc(IntPtr hWnd, IntPtr lParam);

            public bool IsWindowEnabled(IntPtr hWnd) => IsWindowEnabledNative(hWnd);

            public bool EnableWindow(IntPtr hWnd, bool enable) => EnableWindowNative(hWnd, enable);

            public IntPtr GetFocus() => GetFocusNative();

            public void SetFocus(IntPtr hWnd) => _ = SetFocusNative(hWnd);

            public bool IsChild(IntPtr hWndParent, IntPtr hWnd) => IsChildNative(hWndParent, hWnd);

            public IntPtr[] GetThreadWindows()
            {
                var windows = new List<IntPtr>();
                _ = EnumThreadWindowsNative(GetCurrentThreadIdNative(), (hWnd, _) =>
                {
                    windows.Add(hWnd);
                    return true;
                }, IntPtr.Zero);
                return windows.ToArray();
            }

            public bool IsWindowVisible(IntPtr hWnd) => IsWindowVisibleNative(hWnd);

            public IntPtr GetOwner(IntPtr hWnd) => GetWindowNative(hWnd, GW_OWNER);

            public bool IsToolWindow(IntPtr hWnd) => (GetWindowLongNative(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0;

            public IntPtr GetActiveWindow() => GetActiveWindowNative();

            [DllImport("user32.dll", EntryPoint = "IsWindowEnabled")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWindowEnabledNative(IntPtr hWnd);

            [DllImport("user32.dll", EntryPoint = "EnableWindow")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool EnableWindowNative(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

            [DllImport("user32.dll", EntryPoint = "GetFocus")]
            private static extern IntPtr GetFocusNative();

            [DllImport("user32.dll", EntryPoint = "SetFocus")]
            private static extern IntPtr SetFocusNative(IntPtr hWnd);

            [DllImport("user32.dll", EntryPoint = "IsChild")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsChildNative(IntPtr hWndParent, IntPtr hWnd);

            [DllImport("user32.dll", EntryPoint = "EnumThreadWindows")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool EnumThreadWindowsNative(int dwThreadId, EnumThreadWndProc lpfn, IntPtr lParam);

            [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
            private static extern int GetCurrentThreadIdNative();

            [DllImport("user32.dll", EntryPoint = "IsWindowVisible")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWindowVisibleNative(IntPtr hWnd);

            [DllImport("user32.dll", EntryPoint = "GetWindow")]
            private static extern IntPtr GetWindowNative(IntPtr hWnd, uint uCmd);

            [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
            private static extern int GetWindowLongNative(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", EntryPoint = "GetActiveWindow")]
            private static extern IntPtr GetActiveWindowNative();
        }
        #endregion
    }
}
