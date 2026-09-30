using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YANF.Script;
using static System.Math;
using static System.Windows.Forms.DialogResult;
using static System.Windows.Forms.FormStartPosition;
using static System.Windows.Forms.FormWindowState;
using static YANF.Script.YANShape;

namespace YANF.Screen
{
    /// <summary>
    /// Base of the Load, Wait and Update overlay screens: progress display, placement over the window they cover,
    /// rounded corners and an animated close.
    /// </summary>
    /// <remarks>
    /// The screens are shown by <see cref="YANLoader"/> and by the legacy services in <c>YANF.Script.Service</c>; they are not part of
    /// the public API (internal since 2.0).
    /// </remarks>
    internal class YANOverlayScreen : MiddleScreen
    {
        #region Fields
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private static bool _is_DwmMissing;
        // Per-thread override of the synchronization context check (null: check)
        [ThreadStatic]
        private static bool? _canAnimateOverride;
        private Form _owner;
        private int _corner;
        private int _fadeDuration = YANLoaderOptions.DEFAULT_FADE_DURATION;
        private bool _is_ShowRequested;
        private bool _is_AsyncFade;
        private Task _fadeIn;
        private Task _closing;
        private Action _beforeClose;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an overlay screen. Used by the derived screens and by the Windows Forms designer.
        /// </summary>
        public YANOverlayScreen()
        {
        }
        #endregion

        #region Properties
        // Corner of the rounded window region, kept on every size change (0: no region)
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int Corner
        {
            get => _corner;
            set
            {
                _corner = Max(0, value);
                SetRoundRegion(this, _corner);
            }
        }

        // Fade-in and fade-out duration in ms (0: none)
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int FadeDuration
        {
            get => _fadeDuration;
            set => _fadeDuration = Max(0, value);
        }

        // The form this screen follows (same thread), null when it has a fixed placement
        internal Form TrackedOwner => _owner;

        // YANLoader has started closing the screen (CloseAnimatedAsync)
        internal bool IsClosing => _closing != null;

        // The tracked owner cannot show an overlay right now (hidden or minimized)
        private bool IsOwnerHidden => _owner.IsDisposed || !_owner.Visible || _owner.WindowState == Minimized;

        // Tests: the thread's synchronization context brings continuations back to it although it is not a
        // WindowsFormsSynchronizationContext (per thread; null checks the context)
        internal static bool? CanAnimateOverride
        {
            get => _canAnimateOverride;
            set => _canAnimateOverride = value;
        }

        // The async fades need the thread's WinForms synchronization context (their Task.Delay continuations come back through it)
        private static bool CanAnimate => _canAnimateOverride ?? SynchronizationContext.Current is WindowsFormsSynchronizationContext;
        #endregion

        #region Overridden
        /// <summary>
        /// Fades the screen out, then closes and disposes it (a call on a disposed screen does nothing).
        /// Call it on the screen's thread; it returns once the screen is disposed.
        /// </summary>
        public override void Frm_Close()
        {
            if (IsDisposed)
            {
                return;
            }
            // 1.0.x behaviour kept (the legacy services' own thread may block here); YANLoader closes with CloseAnimatedAsync
            Untrack();
            DialogResult = OK;
            this.FadeOut();
            Dispose();
        }

        /// <summary>
        /// Fades the screen in once it is on screen, then raises Shown (the order of the 1.0.x Shown handler).
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            if (!IsDisposed && !IsClosing)
            {
                if (!_is_AsyncFade)
                {
                    // Legacy services and screens created by the caller: the 1.0.x blocking fade
                    this.FadeIn();
                }
                else if (CanAnimate)
                {
                    _fadeIn = this.FadeToAsync(1, _fadeDuration);
                }
                else
                {
                    Opacity = 1;
                }
            }
            base.OnShown(e);
        }

        /// <summary>
        /// Rebuilds the rounded corners for the new size.
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (_corner > 0)
            {
                SetRoundRegion(this, _corner);
            }
        }

        /// <summary>
        /// Stops following the owner.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Untrack();
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Stops following the owner, then releases the screen.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Untrack();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Shows the progress of the running work. The base screen shows no progress (the Wait screen uses it as is).
        /// Called on the screen's thread.
        /// </summary>
        /// <param name="percent">Progress in percent, normally 0 to 100 (a progress bar is clamped to that range).</param>
        /// <param name="detail">Detail text, for example "68 MB / 137 MB" (shown by the Update screen; null shows none).</param>
        protected internal virtual void SetProgress(int percent, string detail)
        {
        }

        /// <summary>
        /// Computes the screen's bounds from the screen bounds of the window it is shown over.
        /// The default covers the whole window (Load and Wait screens); the Update screen centres itself on it.
        /// </summary>
        /// <param name="ownerBounds">Bounds of the window, in screen coordinates.</param>
        /// <returns>The screen's bounds, in screen coordinates.</returns>
        protected virtual Rectangle ComputeBounds(Rectangle ownerBounds) => ownerBounds;

        // Legacy services: fixed placement from a snapshot of the caller's bounds (the screen runs on its own thread and never follows a window of another thread)
        internal void PlaceAt(Rectangle ownerBounds)
        {
            StartPosition = Manual;
            Bounds = ComputeBounds(ownerBounds);
        }

        // Legacy Load and Wait screens: fixed placement, top-most as asked, rounded corners
        internal void PlaceAt(Rectangle ownerBounds, int corner, bool isTop)
        {
            PlaceAt(ownerBounds);
            TopMost = isTop;
            Corner = corner;
        }

        // YANLoader (same thread only): follow the owner's bounds, visibility and minimized state until this screen closes; fade without blocking
        internal void Track(Form owner)
        {
            Untrack();
            _owner = owner;
            _is_AsyncFade = true;
            StartPosition = Manual;
            owner.LocationChanged += Owner_Changed;
            owner.SizeChanged += Owner_Changed;
            owner.Resize += Owner_Changed;
            owner.VisibleChanged += Owner_Changed;
            Follow();
        }

        // Show owned by the tracked owner: now, or as soon as the owner is visible and not minimized
        internal void ShowOverOwner()
        {
            if (_owner == null || _owner.IsDisposed || IsDisposed || IsClosing)
            {
                return;
            }
            _is_ShowRequested = true;
            if (IsOwnerHidden)
            {
                Owner = _owner;
                return;
            }
            Bounds = ComputeBounds(ScreenBoundsOf(_owner));
            Show(_owner);
        }

        // YANLoader: fade out without blocking the thread, then run beforeClose, close and dispose; every call returns the same task
        // (only the first beforeClose is kept). Without a visible window or a WinForms synchronization context the screen closes at
        // once (and a failure is thrown to the caller)
        internal Task CloseAnimatedAsync(Action beforeClose)
        {
            if (_closing != null)
            {
                return _closing;
            }
            _beforeClose = beforeClose;
            if (_fadeDuration > 0 && IsHandleCreated && Visible && Opacity > 0 && CanAnimate)
            {
                return _closing = FadeOutAndCloseAsync();
            }
            _closing = Task.CompletedTask;
            CloseNow();
            return _closing;
        }

        // Let a running fade-in finish (it cannot be cut short), fade out from there, then close (also after a failed fade)
        private async Task FadeOutAndCloseAsync()
        {
            try
            {
                if (_fadeIn != null)
                {
                    await _fadeIn;
                }
                if (!IsDisposed)
                {
                    await this.FadeToAsync(0, _fadeDuration);
                }
            }
            finally
            {
                CloseNow();
            }
        }

        // Run beforeClose once (while the window is still there), then close (FormClosing and FormClosed are raised) and dispose
        private void CloseNow()
        {
            Untrack();
            var beforeClose = _beforeClose;
            _beforeClose = null;
            try
            {
                beforeClose?.Invoke();
            }
            finally
            {
                if (!IsDisposed)
                {
                    Close();
                    Dispose();
                }
            }
        }

        // Owner moved, resized, minimized, restored, hidden or shown
        private void Owner_Changed(object sender, EventArgs e) => Follow();

        // Match the owner: bounds while it can be seen, hidden while it is minimized or hidden
        private void Follow()
        {
            if (_owner == null || IsDisposed)
            {
                return;
            }
            if (IsOwnerHidden)
            {
                if (Visible)
                {
                    Visible = false;
                }
                return;
            }
            Bounds = ComputeBounds(ScreenBoundsOf(_owner));
            if (_is_ShowRequested && !Visible && !IsClosing)
            {
                if (Owner == null)
                {
                    Show(_owner);
                }
                else
                {
                    Visible = true;
                }
            }
        }

        // Visible bounds of a form in screen coordinates. A top-level form with a border: the frame that DWM draws (Windows 10 and later
        // add invisible resize borders to the window rectangle, and a maximized window reaches past its display). An MDI child or
        // embedded form: its bounds, which are in client coordinates of its parent
        internal static Rectangle ScreenBoundsOf(Form frm)
        {
            if (frm.Parent != null)
            {
                return frm.Parent.RectangleToScreen(frm.Bounds);
            }
            var bounds = frm.Bounds;
            // A frame outside the window rectangle is in other coordinates (a DPI-virtualized window): keep the window rectangle then
            return frm.FormBorderStyle != FormBorderStyle.None && frm.IsHandleCreated && TryGetFrameBounds(frm.Handle, out var frame) && bounds.Contains(frame) ? frame : bounds;
        }

        // The DWM frame of a window in screen coordinates; false where DWM does not answer (composition off, no dwmapi as on mono)
        private static bool TryGetFrameBounds(IntPtr hwnd, out Rectangle frame)
        {
            frame = Rectangle.Empty;
            if (_is_DwmMissing)
            {
                return false;
            }
            try
            {
                if (DwmGetWindowAttribute(hwnd, DWMWA_EXTENDED_FRAME_BOUNDS, out var rect, Marshal.SizeOf(typeof(RECT))) != 0)
                {
                    return false;
                }
                frame = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
                return frame.Width > 0 && frame.Height > 0;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                _is_DwmMissing = true;
                return false;
            }
        }

        // Stop following the owner
        private void Untrack()
        {
            var owner = _owner;
            if (owner == null)
            {
                return;
            }
            _owner = null;
            owner.LocationChanged -= Owner_Changed;
            owner.SizeChanged -= Owner_Changed;
            owner.Resize -= Owner_Changed;
            owner.VisibleChanged -= Owner_Changed;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);
        #endregion

        #region Nested types
        /// <summary>
        /// A Win32 RECT.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        #endregion
    }
}
