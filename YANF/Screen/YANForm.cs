using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Math;
using static System.Windows.Forms.FormWindowState;
using static YANF.Control.YANPaint;
using static YANF.Script.YANShape;

namespace YANF.Screen
{
    using WinControl = System.Windows.Forms.Control;
    using WinScreen = System.Windows.Forms.Screen;

    /// <summary>
    /// Base class for borderless forms that draw their own title bar (custom chrome): rounded corners, a drop shadow, resize edges, a
    /// caption band that moves the window, and maximizing that keeps the taskbar visible. Every feature is on by default (the caption band
    /// excepted) and can be turned off in the designer.
    /// </summary>
    /// <remarks>
    /// <para>Derive a form from YANForm instead of Form (the designer's Inherited Form works too). It starts borderless
    /// (<see cref="FormBorderStyle"/> None); the chrome applies to a top-level form while it stays borderless, and with another border style
    /// the form behaves like a plain Form. Sizes in pixels are 96-dpi logical pixels, scaled to the DPI of the window.</para>
    /// <para>A borderless window also gets the system menu and the minimize and maximize window styles that <see cref="Form.ControlBox"/>,
    /// <see cref="Form.MinimizeBox"/> and <see cref="Form.MaximizeBox"/> ask for, and only those (a borderless Form has no system menu and no
    /// minimize style, and the maximize style whatever MaximizeBox says), so that clicking its taskbar button minimizes and restores it and its
    /// taskbar menu offers them. None of this is drawn: the form draws its own title bar and buttons.</para>
    /// <para>A maximized borderless Form covers the whole monitor, taskbar included; a YANForm maximizes to the working area of its monitor
    /// (it sets <see cref="Form.MaximizedBounds"/> when Windows asks for the maximized size). A derived form that wants the whole screen, a
    /// kiosk for example, sets MaximizedBounds itself: YANForm leaves a value it did not set alone. Along an auto-hide taskbar the maximized
    /// window stops 2 pixels short of the monitor edge: Windows takes a window that covers the whole monitor for a full-screen one, and the
    /// hidden taskbar would no longer come up under the mouse.</para>
    /// <para>Windows does not apply Aero Snap to a borderless window (it needs a sizable frame): no snapping to a screen edge or half by
    /// dragging or with Win+Left and Win+Right, and no dragging a maximized window down to restore it. Double-clicking the caption, the
    /// taskbar button and <see cref="Form.WindowState"/> maximize and restore it.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public partial class MainForm : YANForm
    /// {
    ///     public MainForm()
    ///     {
    ///         InitializeComponent();
    ///         Padding = new Padding(6);            // keeps the resize edges (ResizeBorderWidth) free of the docked panels
    ///         CaptionHeight = 32;                  // the top 32 px move the window (also settable in the designer)
    ///         RegisterCaptionControl(pnlHeader);   // and so does the header panel (Dock = Top) that covers them
    ///     }
    /// }
    /// </code>
    /// </example>
    public class YANForm : Form
    {
        #region Fields
        private const int DEFAULT_CORNER_RADIUS = 8;
        private const int DEFAULT_RESIZE_BORDER_WIDTH = 6;
        private const int WM_GETMINMAXINFO = 0x24;
        private const int WM_WINDOWPOSCHANGING = 0x46;
        private const int WM_NCHITTEST = 0x84;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int WM_NCLBUTTONDBLCLK = 0xA3;
        private const int WM_DPICHANGED = 0x2E0;
        private const int HTCLIENT = 1;
        private const int HTCAPTION = 2;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int CS_DROPSHADOW = 0x20000;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;
        private const int WS_SYSMENU = 0x80000;
        private const int SWP_NOSIZE = 0x1;
        private const int SWP_NOMOVE = 0x2;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_DEFAULT = 0;
        private const int DWMWCP_ROUND = 2;
        private const int WINDOWS_11_BUILD = 22000;
        private const int ABM_GETSTATE = 0x4;
        private const int ABM_GETAUTOHIDEBAREX = 0xB;
        private const int ABS_AUTOHIDE = 0x1;
        private const int ABE_LEFT = 0;
        private const int ABE_TOP = 1;
        private const int ABE_RIGHT = 2;
        private const int ABE_BOTTOM = 3;
        // The gap left along an auto-hide taskbar by a maximized window (Chromium's kAutoHideTaskbarThicknessPx)
        private const int AUTO_HIDE_TASKBAR_GAP = 2;
        // Windows 11 or later (build 22000), where DWM rounds a window on request
        private static readonly Lazy<bool> _is_Windows11 = new(DetectWindows11);
        // dwmapi.dll or its function cannot be called (no DWM, mono)
        private static bool _is_DwmMissing;
        // user32.dll cannot be called (mono)
        private static bool _is_User32Missing;
        // shell32.dll cannot be called (mono)
        private static bool _is_Shell32Missing;
        // Per-thread override of the Windows 11 rounding support (null: detect)
        [ThreadStatic]
        private static bool? _systemCornersOverride;
        // Per-thread stand-in for the DWM corner preference call (null: call DWM)
        [ThreadStatic]
        private static Func<IntPtr, int, int> _cornerPreferenceOverride;
        private readonly List<WinControl> _captionControls = new();
        private bool _is_RoundedCorners = true;
        private bool _is_DropShadow = true;
        private bool _is_Resizable = true;
        private bool _is_SystemRounded;
        private int _cornerRadius = DEFAULT_CORNER_RADIUS;
        private int _resizeBorderWidth = DEFAULT_RESIZE_BORDER_WIDTH;
        private int _captionHeight;
        // The DWM corner preference set on the current handle (null: none, or DWM refused it)
        private int? _cornerPreference;
        // Whether the current handle was created with the class shadow (null: no handle)
        private bool? _handleClassShadow;
        // The region that rounds the window where DWM does not, with the size and device radius it was built for
        private Region _cornerRegion;
        private Size _cornerRegionSize;
        private int _cornerRegionRadius;
        // The MaximizedBounds that this class set (empty: none)
        private Rectangle _autoMaximizedBounds;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a borderless form with rounded corners, a drop shadow and resize edges.
        /// </summary>
        public YANForm() => base.FormBorderStyle = FormBorderStyle.None;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the border style of the form. A YANForm is borderless by default (<see cref="FormBorderStyle.None"/>): it draws its
        /// own title bar, and the rounded corners, the shadow, the resize edges and the caption band apply only while it stays borderless.
        /// </summary>
        [Category("Appearance"), Description("The border style of the form. None (the default) keeps the chrome of YANForm: rounded corners, shadow, resize edges and caption band.")]
        [DefaultValue(FormBorderStyle.None)]
        public new FormBorderStyle FormBorderStyle
        {
            get => base.FormBorderStyle;
            // the chrome follows in OnStyleChanged, also when the style is set through a Form reference
            set => base.FormBorderStyle = value;
        }

        /// <summary>
        /// Gets or sets whether the corners of the borderless window are rounded (true by default).
        /// </summary>
        /// <remarks>
        /// <para>On Windows 11 (build 22000 and later) Windows rounds the window itself (DWMWA_WINDOW_CORNER_PREFERENCE): anti-aliased corners
        /// of the system radius (8 px at 96 dpi) with the system border and shadow. On older Windows, or while DWM refuses the call for the
        /// window (it is asked again at the next size change and for a new handle), the window is cut with a region of
        /// <see cref="CornerRadius"/> (aliased edges), rebuilt when the size or the DPI changes; the form's
        /// <see cref="WinControl.Region"/> then belongs to this feature. A maximized window has square corners.</para>
        /// <para>A translucent form (<see cref="Form.Opacity"/> below 1 or a <see cref="Form.TransparencyKey"/>, as during a fade) is a
        /// layered window, and whether Windows 11 keeps rounding and shadowing a layered window has not been verified on every build: check
        /// the look on the target Windows version when a form stays translucent. The corners are applied again whenever the window gets a new
        /// handle.</para>
        /// </remarks>
        [Category("YAN Appearance"), Description("Rounds the corners of the borderless window: Windows 11 rounds it itself (anti-aliased, with the system shadow), older Windows cut it with a region of CornerRadius.")]
        [DefaultValue(true)]
        public bool RoundedCorners
        {
            get => _is_RoundedCorners;
            set
            {
                if (_is_RoundedCorners != value)
                {
                    _is_RoundedCorners = value;
                    UpdateChrome();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius in pixels at 96 dpi (8 by default, the radius of Windows 11) of the region that rounds the window
        /// where Windows does not round it. Windows 11 draws its own radius; 0 gives square corners everywhere. A negative value is stored as 0.
        /// </summary>
        [Category("YAN Appearance"), Description("The corner radius (pixels at 96 dpi) used where Windows does not round the window itself (before Windows 11); 0 gives square corners.")]
        [DefaultValue(DEFAULT_CORNER_RADIUS)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                value = Max(0, value);
                if (_cornerRadius != value)
                {
                    _cornerRadius = value;
                    UpdateChrome();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the borderless window casts a shadow (true by default).
        /// </summary>
        /// <remarks>
        /// Before Windows 11, and on Windows 11 when the corners are not rounded, the shadow comes from the CS_DROPSHADOW class style: it is
        /// rectangular and shows only while the Windows setting "Show shadows under windows" is on. A window rounded by Windows 11 has the system
        /// shadow whatever this property says. Changing the class style gives an existing window a new handle.
        /// </remarks>
        [Category("YAN Appearance"), Description("Casts a shadow under the borderless window (the class drop shadow; a window rounded by Windows 11 always has the system shadow).")]
        [DefaultValue(true)]
        public bool DropShadow
        {
            get => _is_DropShadow;
            set
            {
                if (_is_DropShadow != value)
                {
                    _is_DropShadow = value;
                    UpdateChrome();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the user can resize the borderless window by dragging its edges and corners (true by default).
        /// </summary>
        /// <remarks>
        /// The edges are a band of <see cref="ResizeBorderWidth"/> inside the window, active while the window is neither maximized nor
        /// minimized. They work where the form itself is under the mouse: a child control that covers an edge (a docked panel) takes the mouse
        /// there, so keep the band free, for example with <see cref="WinControl.Padding"/>. <see cref="Form.MinimumSize"/> and
        /// <see cref="Form.MaximumSize"/> limit the size as usual, and a direction that cannot change (a minimum width equal to the maximum
        /// width, or AutoSize with GrowAndShrink) has no edge.
        /// </remarks>
        [Category("YAN Behavior"), Description("Lets the user resize the borderless window by dragging its edges and corners (a band of ResizeBorderWidth inside the window).")]
        [DefaultValue(true)]
        public bool Resizable
        {
            get => _is_Resizable;
            set => _is_Resizable = value;
        }

        /// <summary>
        /// Gets or sets the width in pixels at 96 dpi (6 by default) of the band along the edges of the window that resizes it
        /// (see <see cref="Resizable"/>); 0 turns the edges off. A negative value is stored as 0.
        /// </summary>
        [Category("YAN Behavior"), Description("The width (pixels at 96 dpi) of the band along the edges that resizes the borderless window; 0 turns resizing off.")]
        [DefaultValue(DEFAULT_RESIZE_BORDER_WIDTH)]
        public int ResizeBorderWidth
        {
            get => _resizeBorderWidth;
            set => _resizeBorderWidth = Max(0, value);
        }

        /// <summary>
        /// Gets or sets the height in pixels at 96 dpi (0 by default: none) of the band at the top of the window that acts as its title bar.
        /// A negative value is stored as 0.
        /// </summary>
        /// <remarks>
        /// <para>Dragging the band moves the window with the Windows move loop, and double-clicking it maximizes or restores the window when
        /// <see cref="Form.MaximizeBox"/> is true. Only the parts of the band where no child control lies act as the caption, and the form gets
        /// no mouse events there; <see cref="RegisterCaptionControl"/> makes a header panel or a title label act as the caption too.</para>
        /// <para>Windows does not apply Aero Snap to a borderless window, which has no sizable frame: dragging the band to a screen edge does
        /// not snap the window, Win+Left and Win+Right do not snap it, and dragging a maximized window down does not restore it.</para>
        /// </remarks>
        [Category("YAN Behavior"), Description("The height (pixels at 96 dpi) of the band at the top of the borderless window that moves it like a title bar; 0 means none.")]
        [DefaultValue(0)]
        public int CaptionHeight
        {
            get => _captionHeight;
            set => _captionHeight = Max(0, value);
        }

        /// <summary>
        /// Lets tests run as if Windows could round the window (Windows 11) or not, whatever the machine (per thread; null detects it).
        /// </summary>
        internal static bool? SystemCornersOverride
        {
            get => _systemCornersOverride;
            set => _systemCornersOverride = value;
        }

        /// <summary>
        /// Lets tests stand in for the DWM corner preference call: gets the window handle and the preference, returns the HRESULT (per thread;
        /// null calls DWM).
        /// </summary>
        internal static Func<IntPtr, int, int> CornerPreferenceOverride
        {
            get => _cornerPreferenceOverride;
            set => _cornerPreferenceOverride = value;
        }

        /// <summary>
        /// True while Windows rounds the current window handle itself (the DWM corner preference was accepted).
        /// </summary>
        internal bool IsSystemRounded => _is_SystemRounded;

        // Windows can round a window itself: Windows 11 with a working DWM corner preference
        private static bool IsSystemRoundingAvailable => _systemCornersOverride ?? (_is_Windows11.Value && !_is_DwmMissing);

        // The chrome applies: a top-level borderless form
        private bool IsChromeActive => TopLevel && base.FormBorderStyle == FormBorderStyle.None;

        // The settings ask for rounded corners
        private bool IsRoundingWanted => _is_RoundedCorners && _cornerRadius > 0;

        // The window class gets CS_DROPSHADOW: a shadow is wanted and Windows 11 does not round (and shadow) the window
        private bool HasClassShadow => _is_DropShadow && IsChromeActive && !(IsRoundingWanted && IsSystemRoundingAvailable);
        #endregion

        #region Overridden
        /// <summary>
        /// Adds the chrome of a borderless top-level window: the CS_DROPSHADOW class style (see <see cref="DropShadow"/>), and the system menu
        /// and the minimize and maximize styles that ControlBox, MinimizeBox and MaximizeBox ask for (and only those).
        /// </summary>
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                if (IsChromeActive)
                {
                    if (HasClassShadow)
                    {
                        cp.ClassStyle |= CS_DROPSHADOW;
                    }
                    // Set or cleared, as Form does for a window with a border: WS_MAXIMIZEBOX is the bit of WS_TABSTOP, which Control sets
                    // for every tab stop, a Form included, and left alone it would let Windows maximize the window although MaximizeBox is
                    // false (a caption double-click through DefWindowProc, Win+Up, the taskbar menu)
                    cp.Style = ControlBox ? cp.Style | WS_SYSMENU : cp.Style & ~WS_SYSMENU;
                    cp.Style = MinimizeBox ? cp.Style | WS_MINIMIZEBOX : cp.Style & ~WS_MINIMIZEBOX;
                    cp.Style = MaximizeBox ? cp.Style | WS_MAXIMIZEBOX : cp.Style & ~WS_MAXIMIZEBOX;
                }
                return cp;
            }
        }

        /// <summary>
        /// Rounds the corners of the new window handle (see <see cref="RoundedCorners"/>).
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            // a new handle has no DWM corner preference yet, and the class style it was created with
            _cornerPreference = null;
            _is_SystemRounded = false;
            _handleClassShadow = HasClassShadow;
            base.OnHandleCreated(e);
            ApplyCorners();
        }

        /// <summary>
        /// Forgets the chrome state of the destroyed window handle.
        /// </summary>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            _handleClassShadow = null;
            _cornerPreference = null;
            _is_SystemRounded = false;
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Keeps the chrome in step with the window styles, however they changed (<see cref="FormBorderStyle"/> set through a
        /// <see cref="Form"/> reference included, and ControlBox, MinimizeBox, MaximizeBox or TopLevel): a class shadow other than the one of
        /// the window handle gives the window a new handle, and the corners are applied again.
        /// </summary>
        protected override void OnStyleChanged(EventArgs e)
        {
            base.OnStyleChanged(e);
            UpdateChrome();
        }

        /// <summary>
        /// Rebuilds the corner region for the new size where Windows does not round the window itself.
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyCorners();
        }

        /// <summary>
        /// Handles the chrome of a borderless top-level window: the resize edges and the caption band (WM_NCHITTEST), the caption
        /// double-click, the maximized bounds that keep the taskbar visible, and the corners after a DPI change.
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCHITTEST:
                    base.WndProc(ref m);
                    m.Result = (IntPtr)ChromeHitTest(unchecked((int)m.Result.ToInt64()), PointFromLParam(m.LParam));
                    break;
                case WM_NCLBUTTONDBLCLK when m.WParam == (IntPtr)HTCAPTION && IsChromeActive && MaximizeBox:
                    // maximize or restore like a title bar (DefWindowProc does it only for a window with the maximize style)
                    WindowState = WindowState == Maximized ? Normal : Maximized;
                    m.Result = IntPtr.Zero;
                    break;
                case WM_GETMINMAXINFO:
                    // Form reads MaximizedBounds while it answers the message
                    UpdateMaximizedBounds(m.HWnd);
                    base.WndProc(ref m);
                    break;
                case WM_WINDOWPOSCHANGING:
                    base.WndProc(ref m);
                    FitMaximizedWindow(m.LParam);
                    break;
                case WM_DPICHANGED:
                    base.WndProc(ref m);
                    ApplyCorners();
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        /// <summary>
        /// Forgets the caption controls (see <see cref="RegisterCaptionControl"/>), disposes the form, then frees the corner region.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var ctrl in _captionControls.ToArray())
                {
                    UnregisterCaptionControl(ctrl);
                }
            }
            base.Dispose(disposing);
            if (disposing)
            {
                // the window is gone: free the corner region, which the form does not dispose (removing it before would show square corners
                // while the window closes)
                _cornerRegion?.Dispose();
                _cornerRegion = null;
            }
        }
        #endregion

        #region Events
        // A press on a caption control: a single left press starts the Windows move loop (it returns when the button is released), a
        // double-click maximizes or restores the form, like on a title bar
        private void CaptionControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || sender is not WinControl { IsDisposed: false } ctrl || IsDisposed || !IsHandleCreated)
            {
                return;
            }
            var lParam = LParamFromPoint(ctrl.PointToScreen(e.Location));
            // the move loop needs the mouse, which the pressed control has captured
            ctrl.Capture = false;
            var m = Message.Create(Handle, e.Clicks > 1 ? WM_NCLBUTTONDBLCLK : WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, lParam);
            WndProc(ref m);
        }

        // A caption control was disposed
        private void CaptionControl_Disposed(object sender, EventArgs e)
        {
            if (sender is WinControl ctrl)
            {
                UnregisterCaptionControl(ctrl);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Makes a control act as the title bar of this form: pressing the left button on it and dragging moves the form with the Windows move
        /// loop, and double-clicking it maximizes or restores the form when <see cref="Form.MaximizeBox"/> is true. As with
        /// <see cref="CaptionHeight"/>, Windows does not snap the borderless form (no Aero Snap).
        /// </summary>
        /// <param name="control">A passive surface, usually a child of this form: a header panel, a title label, a logo.</param>
        /// <remarks>
        /// Registering a control again has no further effect, and a disposed control is ignored. The control is forgotten when it is disposed
        /// or passed to <see cref="UnregisterCaptionControl"/>. While Windows moves the form, the control gets no MouseUp or Click for that
        /// press. The double-click needs a control that reports double-clicks (a Panel, a Label or a PictureBox do; a Button does not).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null.</exception>
        public void RegisterCaptionControl(WinControl control)
        {
            if (control is null)
            {
                throw new ArgumentNullException(nameof(control));
            }
            if (control.IsDisposed || _captionControls.Contains(control))
            {
                return;
            }
            _captionControls.Add(control);
            control.MouseDown += CaptionControl_MouseDown;
            control.Disposed += CaptionControl_Disposed;
        }

        /// <summary>
        /// Stops a control from acting as the title bar of this form (undoes <see cref="RegisterCaptionControl"/>; does nothing for a control
        /// that is not registered).
        /// </summary>
        /// <param name="control">A control passed to <see cref="RegisterCaptionControl"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="control"/> is null.</exception>
        public void UnregisterCaptionControl(WinControl control)
        {
            if (control is null)
            {
                throw new ArgumentNullException(nameof(control));
            }
            if (_captionControls.Remove(control))
            {
                control.MouseDown -= CaptionControl_MouseDown;
                control.Disposed -= CaptionControl_Disposed;
            }
        }

        /// <summary>
        /// True when the control acts as the title bar of this form (<see cref="RegisterCaptionControl"/>).
        /// </summary>
        internal bool IsCaptionControl(WinControl control) => _captionControls.Contains(control);

        /// <summary>
        /// The maximized bounds that keep the taskbar visible: the area of <see cref="GetMaximizedArea(Rectangle, Rectangle, AnchorStyles)"/>,
        /// placed relative to the monitor's top-left corner, because Windows reads MINMAXINFO.ptMaxPosition for the primary monitor and moves it
        /// to the monitor the window maximizes on (on the primary monitor this is the area itself).
        /// </summary>
        internal static Rectangle GetMaximizedBounds(Rectangle monitor, Rectangle workingArea, AnchorStyles autoHideEdges = AnchorStyles.None)
        {
            var area = GetMaximizedArea(monitor, workingArea, autoHideEdges);
            return new Rectangle(area.X - monitor.X, area.Y - monitor.Y, area.Width, area.Height);
        }

        /// <summary>
        /// The screen rectangle that a maximized borderless window fills on a monitor: the working area, pulled in by 2 pixels from each edge of
        /// the monitor where an auto-hide taskbar waits (<paramref name="autoHideEdges"/>) and the working area reaches that edge. Windows takes
        /// a window that covers the whole monitor for a full-screen one, and an auto-hide taskbar then no longer comes up under the mouse.
        /// </summary>
        internal static Rectangle GetMaximizedArea(Rectangle monitor, Rectangle workingArea, AnchorStyles autoHideEdges)
        {
            int left = workingArea.Left, top = workingArea.Top, right = workingArea.Right, bottom = workingArea.Bottom;
            if ((autoHideEdges & AnchorStyles.Left) != 0 && left <= monitor.Left)
            {
                left = monitor.Left + AUTO_HIDE_TASKBAR_GAP;
            }
            if ((autoHideEdges & AnchorStyles.Top) != 0 && top <= monitor.Top)
            {
                top = monitor.Top + AUTO_HIDE_TASKBAR_GAP;
            }
            if ((autoHideEdges & AnchorStyles.Right) != 0 && right >= monitor.Right)
            {
                right = monitor.Right - AUTO_HIDE_TASKBAR_GAP;
            }
            if ((autoHideEdges & AnchorStyles.Bottom) != 0 && bottom >= monitor.Bottom)
            {
                bottom = monitor.Bottom - AUTO_HIDE_TASKBAR_GAP;
            }
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        /// <summary>
        /// The screen rectangle that a maximized borderless window fills on the screen (see
        /// <see cref="GetMaximizedArea(Rectangle, Rectangle, AnchorStyles)"/>), with the auto-hide taskbars that the shell reports.
        /// </summary>
        internal static Rectangle GetMaximizedArea(WinScreen screen)
        {
            var monitor = screen.Bounds;
            var work = screen.WorkingArea;
            // only a window that covers the whole monitor hides an auto-hide taskbar: ask the shell (another process) only then
            return GetMaximizedArea(monitor, work, work == monitor ? GetAutoHideEdges(monitor) : AnchorStyles.None);
        }

        // Apply the settings to the window: a class style other than the one the handle was created with needs a new handle (which applies
        // the corners); the corners change in place
        private void UpdateChrome()
        {
            // no handle, or one that OnHandleCreated has not reached yet (it applies the corners)
            if (!IsHandleCreated || IsDisposed || Disposing || _handleClassShadow is not bool handleClassShadow)
            {
                return;
            }
            if (handleClassShadow != HasClassShadow)
            {
                RecreateHandle();
            }
            else
            {
                ApplyCorners();
            }
        }

        // Round the window: through DWM where Windows can (Windows 11), with a window region elsewhere or when DWM refuses
        private void ApplyCorners()
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }
            var round = IsChromeActive && IsRoundingWanted;
            var systemRounded = false;
            if (IsSystemRoundingAvailable && (round || _cornerPreference != null))
            {
                var preference = round ? DWMWCP_ROUND : DWMWCP_DEFAULT;
                if (_cornerPreference != preference)
                {
                    _cornerPreference = TrySetCornerPreference(Handle, preference) ? preference : null;
                }
                systemRounded = round && _cornerPreference == DWMWCP_ROUND;
            }
            _is_SystemRounded = systemRounded;
            if (round && !systemRounded)
            {
                ApplyRegionCorners();
            }
            else
            {
                RemoveRegionCorners();
            }
        }

        // Cut the corners with a region of CornerRadius at the window's DPI (square while maximized; kept while minimized for the restore)
        private void ApplyRegionCorners()
        {
            switch (WindowState)
            {
                case Minimized:
                    return;
                case Maximized:
                    RemoveRegionCorners();
                    return;
            }
            var size = Size;
            var radius = LogicalToDevice(this, _cornerRadius);
            if (_cornerRegion != null && ReferenceEquals(Region, _cornerRegion) && _cornerRegionSize == size && _cornerRegionRadius == radius)
            {
                return;
            }
            // a path region instead of CreateRoundRectRgn, which leaves out the right and bottom edges of the window
            using var path = RoundedRect(new RectangleF(0, 0, size.Width, size.Height), radius);
            var region = path == null ? null : new Region(path);
            SetRegion(this, region);
            _cornerRegion = region;
            _cornerRegionSize = size;
            _cornerRegionRadius = radius;
        }

        // Remove the corner region (only the one this class set: a region of the derived form stays)
        private void RemoveRegionCorners()
        {
            if (_cornerRegion != null && ReferenceEquals(Region, _cornerRegion))
            {
                SetRegion(this, (Region)null);
            }
            _cornerRegion = null;
        }

        /// <summary>
        /// The answer to WM_NCHITTEST: where Windows answers HTCLIENT in a borderless top-level window, the resize edge or corner, the
        /// caption band or the client area under the screen point; any other answer of Windows is kept.
        /// </summary>
        internal int ChromeHitTest(int systemHit, Point pt)
        {
            var bounds = Bounds;
            if (systemHit != HTCLIENT || !IsChromeActive || !bounds.Contains(pt))
            {
                return systemHit;
            }
            if (_is_Resizable && WindowState == Normal && !(AutoSize && AutoSizeMode == AutoSizeMode.GrowAndShrink))
            {
                var border = LogicalToDevice(this, _resizeBorderWidth);
                if (border > 0)
                {
                    var min = MinimumSize;
                    var max = MaximumSize;
                    var canSizeX = max.Width <= 0 || min.Width < max.Width;
                    var canSizeY = max.Height <= 0 || min.Height < max.Height;
                    var left = canSizeX && pt.X < bounds.Left + border;
                    var right = canSizeX && !left && pt.X >= bounds.Right - border;
                    var top = canSizeY && pt.Y < bounds.Top + border;
                    var bottom = canSizeY && !top && pt.Y >= bounds.Bottom - border;
                    if (top)
                    {
                        return left ? HTTOPLEFT : right ? HTTOPRIGHT : HTTOP;
                    }
                    if (bottom)
                    {
                        return left ? HTBOTTOMLEFT : right ? HTBOTTOMRIGHT : HTBOTTOM;
                    }
                    if (left || right)
                    {
                        return left ? HTLEFT : HTRIGHT;
                    }
                }
            }
            return pt.Y < bounds.Top + LogicalToDevice(this, _captionHeight) ? HTCAPTION : HTCLIENT;
        }

        // A maximized borderless window covers the whole monitor, taskbar included: maximize it to the working area of its monitor instead,
        // unless the derived form set MaximizedBounds itself
        private void UpdateMaximizedBounds(IntPtr hwnd)
        {
            var current = MaximizedBounds;
            if (!current.IsEmpty && current != _autoMaximizedBounds)
            {
                return;
            }
            var bounds = Rectangle.Empty;
            if (IsChromeActive)
            {
                var screen = WinScreen.FromHandle(hwnd);
                bounds = GetMaximizedBounds(screen.Bounds, GetMaximizedArea(screen));
            }
            _autoMaximizedBounds = bounds;
            MaximizedBounds = bounds;
        }

        // Windows enlarges the maximized size when the monitor is larger than the primary one: keep the maximized window inside the maximized
        // area of its monitor (only with the MaximizedBounds that this class set)
        private void FitMaximizedWindow(IntPtr lParam)
        {
            if (lParam == IntPtr.Zero || !IsChromeActive || _autoMaximizedBounds.IsEmpty || MaximizedBounds != _autoMaximizedBounds)
            {
                return;
            }
            var pos = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);
            if ((pos.flags & (SWP_NOSIZE | SWP_NOMOVE)) != 0 || !IsMaximizedWindow(pos.hwnd))
            {
                return;
            }
            var rect = new Rectangle(pos.x, pos.y, pos.cx, pos.cy);
            var fit = Rectangle.Intersect(rect, GetMaximizedArea(WinScreen.FromPoint(rect.Location)));
            if (fit.IsEmpty || fit == rect)
            {
                return;
            }
            pos.x = fit.X;
            pos.y = fit.Y;
            pos.cx = fit.Width;
            pos.cy = fit.Height;
            Marshal.StructureToPtr(pos, lParam, false);
        }

        // The window is maximized (IsZoomed is already true while WM_WINDOWPOSCHANGING maximizes it; WindowState is updated afterwards)
        private bool IsMaximizedWindow(IntPtr hwnd)
        {
            if (!_is_User32Missing)
            {
                try
                {
                    return NativeMethods.IsZoomed(hwnd);
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                {
                    _is_User32Missing = true;
                }
            }
            return WindowState == Maximized;
        }

        // Ask DWM for a corner preference; false where DWM is missing or refuses it for this window (a refusal concerns this window only: the
        // caller asks again later)
        private static bool TrySetCornerPreference(IntPtr hwnd, int preference)
        {
            if (_cornerPreferenceOverride is { } standIn)
            {
                return standIn(hwnd, preference) == 0;
            }
            if (_is_DwmMissing)
            {
                return false;
            }
            try
            {
                return NativeMethods.DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int)) == 0;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                _is_DwmMissing = true;
                return false;
            }
        }

        // The edges of a monitor where an auto-hide taskbar waits, as the shell reports them (none where it cannot be asked)
        private static AnchorStyles GetAutoHideEdges(Rectangle monitor)
        {
            if (_is_Shell32Missing)
            {
                return AnchorStyles.None;
            }
            try
            {
                var state = new NativeMethods.APPBARDATA
                {
                    cbSize = Marshal.SizeOf<NativeMethods.APPBARDATA>()
                };
                if ((NativeMethods.SHAppBarMessage(ABM_GETSTATE, ref state).ToInt64() & ABS_AUTOHIDE) == 0)
                {
                    return AnchorStyles.None;
                }
                return Edge(ABE_LEFT, AnchorStyles.Left) | Edge(ABE_TOP, AnchorStyles.Top) | Edge(ABE_RIGHT, AnchorStyles.Right) | Edge(ABE_BOTTOM, AnchorStyles.Bottom);
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                _is_Shell32Missing = true;
                return AnchorStyles.None;
            }

            // The side when an auto-hide bar waits on this edge of the monitor
            AnchorStyles Edge(int edge, AnchorStyles side)
            {
                var data = new NativeMethods.APPBARDATA
                {
                    cbSize = Marshal.SizeOf<NativeMethods.APPBARDATA>(),
                    uEdge = edge,
                    rc = new NativeMethods.RECT(monitor)
                };
                return NativeMethods.SHAppBarMessage(ABM_GETAUTOHIDEBAREX, ref data) != IntPtr.Zero ? side : AnchorStyles.None;
            }
        }

        // The real Windows build (RtlGetVersion is not subject to the compatibility shims that make Environment.OSVersion report Windows 8
        // to an application without a manifest)
        private static bool DetectWindows11()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }
            try
            {
                var info = new NativeMethods.OSVERSIONINFOW
                {
                    dwOSVersionInfoSize = Marshal.SizeOf<NativeMethods.OSVERSIONINFOW>()
                };
                return NativeMethods.RtlGetVersion(ref info) == 0 && info.dwMajorVersion >= 10 && info.dwBuildNumber >= WINDOWS_11_BUILD;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                return false;
            }
        }

        // The screen point of a mouse message (signed: monitors left of or above the primary one have negative coordinates)
        private static Point PointFromLParam(IntPtr lParam)
        {
            var value = unchecked((int)lParam.ToInt64());
            return new Point(unchecked((short)value), unchecked((short)(value >> 16)));
        }

        // The lParam of a mouse message at a screen point
        private static IntPtr LParamFromPoint(Point pt) => (IntPtr)unchecked((pt.Y << 16) | (pt.X & 0xFFFF));
        #endregion

        #region Nested types
        // The native calls of YANForm; every caller handles a missing DLL or function
        private static class NativeMethods
        {
            [DllImport("dwmapi.dll")]
            internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsZoomed(IntPtr hWnd);

            [DllImport("ntdll.dll")]
            internal static extern int RtlGetVersion(ref OSVERSIONINFOW lpVersionInformation);

            [DllImport("shell32.dll")]
            internal static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

            // A Win32 APPBARDATA
            [StructLayout(LayoutKind.Sequential)]
            internal struct APPBARDATA
            {
                public int cbSize;
                public IntPtr hWnd;
                public int uCallbackMessage;
                public int uEdge;
                public RECT rc;
                public IntPtr lParam;
            }

            // A Win32 RECT
            [StructLayout(LayoutKind.Sequential)]
            internal struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;

                public RECT(Rectangle rect)
                {
                    left = rect.Left;
                    top = rect.Top;
                    right = rect.Right;
                    bottom = rect.Bottom;
                }
            }

            // A Win32 WINDOWPOS
            [StructLayout(LayoutKind.Sequential)]
            internal struct WINDOWPOS
            {
                public IntPtr hwnd;
                public IntPtr hwndInsertAfter;
                public int x;
                public int y;
                public int cx;
                public int cy;
                public int flags;
            }

            // A Win32 OSVERSIONINFOW
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct OSVERSIONINFOW
            {
                public int dwOSVersionInfoSize;
                public int dwMajorVersion;
                public int dwMinorVersion;
                public int dwBuildNumber;
                public int dwPlatformId;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string szCSDVersion;
            }
        }
        #endregion
    }
}
