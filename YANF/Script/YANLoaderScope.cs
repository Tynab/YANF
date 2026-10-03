using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YANF.Screen;
using static System.Math;
using static System.Windows.Forms.FormWindowState;
using static YANF.Script.YANLoaderKind;
using Timer = System.Windows.Forms.Timer;
using WinControl = System.Windows.Forms.Control;

namespace YANF.Script
{
    /// <summary>
    /// A Load, Wait or Update screen shown over a form by <see cref="YANLoader.Show(Form, YANLoaderOptions)"/>.
    /// While the scope is open the owner form takes no mouse or keyboard input; dispose the scope (a <c>using</c> block)
    /// to close the screen and give the owner its input back. When several scopes are open on one form, it gets its input back
    /// when the last one is disposed.
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
        // This scope's hold on the owner (shared with the other scopes open on it), null when it holds none
        private OwnerBlock _block;
        private OwnerInputFilter _filter;
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

        // This scope holds the owner window disabled (alone or with other scopes) and has not let go yet
        internal bool IsOwnerBlocked => _block != null;
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

        // A failed close is reported like any other UI-thread failure: rethrown on the UI thread, by its message loop
        // (Application.ThreadException). Not with an async void method: where the thread has no WinForms SynchronizationContext (WinForms
        // leaves a plain one once its outermost message loop ends) that rethrows on the thread pool, which ends the process
        private void Observe(Task task) => _ = task.ContinueWith(t =>
        {
            var error = t.Exception.InnerException;
            _ = TryPost(_ => ExceptionDispatchInfo.Capture(error).Throw());
        }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

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
        // screen was the active window. The task ends after that, with the failure of the close. What follows the fade-out is posted to
        // the UI thread: an await would continue on the thread pool where the thread has no WinForms SynchronizationContext
        private Task CloseScreenAsync(bool isActive)
        {
            Task closing;
            try
            {
                closing = _scr.CloseAnimatedAsync(ReleaseOnUi);
            }
            catch (Exception ex)
            {
                closing = Task.FromException(ex);
            }
            if (closing.IsCompleted)
            {
                return AfterClose(closing, isActive);
            }
            var done = new TaskCompletionSource<Task>();
            _ = closing.ContinueWith(t =>
            {
                if (!TryPost(_ => done.TrySetResult(AfterClose(t, isActive))))
                {
                    // The owner's window or its thread is gone: no owner to give back
                    _ = done.TrySetResult(t);
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return done.Task.Unwrap();
        }

        // UI thread: after the close, also a failed one that did not give the owner back: give it back and bring it forward if the screen
        // was the active window. Returns the close (or the failure of this step)
        private Task AfterClose(Task closing, bool isActive)
        {
            try
            {
                Release();
                if (isActive && !_is_OwnerGone && !_owner.IsDisposed && _owner.Visible && Form.ActiveForm == null)
                {
                    _owner.Activate();
                }
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
            return closing;
        }

        // The screen gives the owner back just before it closes, on the UI thread (elsewhere only when its window was gone already)
        private void ReleaseOnUi()
        {
            if (Thread.CurrentThread == _thread)
            {
                Release();
            }
            else
            {
                _ = TryPost(_ => Release());
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

        // Disable the owner window like a modal dialog does (Control.Enabled would grey out every control), and drop the keys and wheel
        // turns sent to it: a disabled window keeps its focused child, and while it is the active window without a focus Windows sends
        // it the keys, which WinForms would still turn into clicks (AcceptButton, CancelButton, the focused button). The scopes open on
        // one owner share its block; an owner disabled by someone else (a modal dialog, the app) is left to them
        private void BlockOwner()
        {
            _block = OwnerBlock.Enter(this);
            if (_block != null)
            {
                Application.AddMessageFilter(_filter ??= new OwnerInputFilter(this));
            }
        }

        // Let go of the owner. The last scope to hold it enables the window again (at once, or once a modal dialog opened meanwhile is
        // gone) and gives the keyboard focus back
        private void UnblockOwner()
        {
            var block = _block;
            if (block == null)
            {
                return;
            }
            _block = null;
            Application.RemoveMessageFilter(_filter);
            var isGone = _is_OwnerGone || _owner.IsDisposed || !_owner.IsHandleCreated;
            if (!block.Leave(isGone) || isGone)
            {
                return;
            }
            _ = Native.EnableWindow(_hwnd, true);
            RestoreFocus(block);
        }

        // Right after the owner is enabled again: give the keyboard focus back when the owner is the active window without it on one of
        // its controls (a disabled window takes no focus). Windows gives the focus to the window it activates: an owner activated while
        // disabled (a scope opened in Load, before Windows first shows and activates the form) holds it itself, as WinForms could not
        // pass it on to the active control then. A top-level owner blocked before it was first on screen whose screen never appeared may
        // not have been activated at all: activate it now, as showing it would have
        private void RestoreFocus(OwnerBlock block)
        {
            var focus = Native.GetFocus();
            if (IsFocusKept(focus, block))
            {
                return;
            }
            if (Native.GetActiveWindow() != _hwnd)
            {
                if (block.IsShown || !_owner.TopLevel || _scr.IsHandleCreated || !_owner.Visible || _owner.WindowState == Minimized || !Native.IsWindowVisible(_hwnd))
                {
                    return;
                }
                // Form.Active then focuses its active control
                _owner.Activate();
                focus = Native.GetFocus();
                if (Native.GetActiveWindow() != _hwnd || IsFocusKept(focus, block))
                {
                    return;
                }
            }
            if (focus != IntPtr.Zero && focus != _hwnd)
            {
                return;
            }
            if (block.Focus != _hwnd && IsInOwner(block.Focus))
            {
                Native.SetFocus(block.Focus);
            }
            else if (_owner.TopLevel && _owner.WindowState != Minimized)
            {
                FocusOwner();
            }
        }

        // The focus is where it belongs once the owner is enabled: on a child window of the owner, or on the owner window itself when it
        // had it before the block (a form without a control that takes the focus)
        private bool IsFocusKept(IntPtr focus, OwnerBlock block) => focus == _hwnd ? block.Focus == _hwnd : IsInOwner(focus);

        // Focus the active owner as WinForms does when a form is activated: its active control, else its first control, else the form.
        // When the owner window holds the focus itself, its WM_SETFOCUS has come and gone: pass the focus on to the active control directly
        private void FocusOwner()
        {
            if (_owner.ActiveControl == null)
            {
                // focuses the control too when the owner window has the focus
                _ = _owner.SelectNextControl(null, true, true, true, false);
            }
            var focus = Native.GetFocus();
            if (focus == _hwnd)
            {
                if (_owner.ActiveControl is { IsHandleCreated: true, Visible: true } active)
                {
                    Native.SetFocus(active.Handle);
                }
            }
            else if (!IsInOwner(focus))
            {
                // WM_SETFOCUS: the form passes the focus on to its active control
                Native.SetFocus(_hwnd);
            }
        }

        // The window is the owner window or one of its child windows (at any depth)
        private bool IsInOwner(IntPtr hwnd) => hwnd != IntPtr.Zero && (hwnd == _hwnd || Native.IsChild(_hwnd, hwnd));

        // The owner window was recreated (RecreateHandle): the new window starts enabled, so block it again
        private void Owner_HandleCreated(object sender, EventArgs e)
        {
            _hwnd = _owner.Handle;
            if (_block != null)
            {
                _block.Focus = IntPtr.Zero;
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
            // Nothing to enable: only this scope's hold and input filter go
            UnblockOwner();
            StopTimer();
            if (!_scr.IsDisposed)
            {
                _scr.Dispose();
            }
        }
        #endregion

        #region Nested types
        /// <summary>
        /// The hold of the open scopes of one owner form on its window: the first scope disables it, the last one enables it again.
        /// Used on the owner's UI thread only.
        /// </summary>
        private sealed class OwnerBlock
        {
            // Per owner form, by reference
            private static readonly ConditionalWeakTable<Form, OwnerBlock> _blocks = new();
            private readonly Form _owner;
            private readonly List<Form> _modalsBefore;
            private int _count;
            private Form _modal;

            private OwnerBlock(Form owner, IntPtr focus, bool isShown)
            {
                _owner = owner;
                Focus = focus;
                IsShown = isShown;
                // Modal dialogs open now disabled the other windows themselves and enable them again when they close
                _modalsBefore = ThreadForms().Where(f => f.Modal && f.Visible).ToList();
            }

            // The focused window inside the owner when the block began (zero: none, or the window was recreated since)
            internal IntPtr Focus { get; set; }

            // The owner window was on screen when the block began; not while Load runs: Windows shows and activates the window after it
            internal bool IsShown { get; }

            // Hold the scope's owner; the first hold disables its window. Null when someone else has disabled it (a modal dialog, the app)
            internal static OwnerBlock Enter(YANLoaderScope scope)
            {
                if (_blocks.TryGetValue(scope._owner, out var block))
                {
                    // Held already (by open scopes, or waiting for a dialog): the window stays disabled, also a window recreated meanwhile
                    if (Native.IsWindowEnabled(scope._hwnd))
                    {
                        _ = Native.EnableWindow(scope._hwnd, false);
                    }
                }
                else
                {
                    if (!Native.IsWindowEnabled(scope._hwnd))
                    {
                        return null;
                    }
                    var focus = Native.GetFocus();
                    block = new OwnerBlock(scope._owner, scope.IsInOwner(focus) ? focus : IntPtr.Zero, Native.IsWindowVisible(scope._hwnd));
                    _ = Native.EnableWindow(scope._hwnd, false);
                    _blocks.Add(scope._owner, block);
                }
                block._count++;
                return block;
            }

            // Let go of the owner; true when this was the last hold and the window is to be enabled now. While a modal dialog opened
            // since the block began is open, the owner stays disabled: the dialog left it alone (it was disabled already), so it would
            // not enable it again either, and the owner would take input behind the dialog. It is enabled once the dialog is gone
            internal bool Leave(bool isOwnerGone)
            {
                if (--_count > 0 || (!isOwnerGone && WaitsForModal(null)))
                {
                    return false;
                }
                StopWaiting();
                _ = _blocks.Remove(_owner);
                return true;
            }

            // A modal dialog opened since the block began is open (other than the one closing): wait until it is gone
            private bool WaitsForModal(Form closing)
            {
                if (_modal != null)
                {
                    return true;
                }
                var modal = ThreadForms().FirstOrDefault(f => f != _owner && f != closing && f.Modal && f.Visible && !_modalsBefore.Contains(f));
                if (modal == null)
                {
                    return false;
                }
                _modal = modal;
                modal.FormClosed += Modal_FormClosed;
                modal.VisibleChanged += Modal_VisibleChanged;
                modal.HandleDestroyed += Modal_HandleDestroyed;
                return true;
            }

            // Stop waiting for a modal dialog
            private void StopWaiting()
            {
                var modal = _modal;
                if (modal == null)
                {
                    return;
                }
                _modal = null;
                modal.FormClosed -= Modal_FormClosed;
                modal.VisibleChanged -= Modal_VisibleChanged;
                modal.HandleDestroyed -= Modal_HandleDestroyed;
            }

            // The awaited dialog closed (before it is hidden, so that the owner can be activated when it goes)
            private void Modal_FormClosed(object sender, FormClosedEventArgs e) => ModalGone(sender);

            // The awaited dialog was hidden (which also ends ShowDialog)
            private void Modal_VisibleChanged(object sender, EventArgs e)
            {
                if (sender is Form { Visible: false })
                {
                    ModalGone(sender);
                }
            }

            // The awaited dialog's window was destroyed
            private void Modal_HandleDestroyed(object sender, EventArgs e)
            {
                if (sender is Form { RecreatingHandle: false })
                {
                    ModalGone(sender);
                }
            }

            // Enable the owner again, unless a scope holds it again (the last one enables it) or another dialog opened meanwhile is open
            private void ModalGone(object sender)
            {
                var modal = _modal;
                if (modal == null || sender != modal)
                {
                    return;
                }
                StopWaiting();
                if (_count > 0 || WaitsForModal(modal))
                {
                    return;
                }
                _ = _blocks.Remove(_owner);
                if (!_owner.IsDisposed && _owner.IsHandleCreated)
                {
                    _ = Native.EnableWindow(_owner.Handle, true);
                }
            }
        }

        /// <summary>
        /// Drops the keyboard and mouse wheel messages sent to the owner window or its child windows while the scope holds the owner.
        /// </summary>
        private sealed class OwnerInputFilter : IMessageFilter
        {
            private const int WM_KEYFIRST = 0x0100;
            private const int WM_KEYLAST = 0x0109;
            private const int WM_MOUSEWHEEL = 0x020A;
            private const int WM_MOUSEHWHEEL = 0x020E;
            private readonly YANLoaderScope _scope;

            internal OwnerInputFilter(YANLoaderScope scope) => _scope = scope;

            public bool PreFilterMessage(ref Message m) => (m.Msg is (>= WM_KEYFIRST and <= WM_KEYLAST) or WM_MOUSEWHEEL or WM_MOUSEHWHEEL)
                && _scope._block != null && Thread.CurrentThread == _scope._thread && _scope.IsInOwner(m.HWnd);
        }

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
