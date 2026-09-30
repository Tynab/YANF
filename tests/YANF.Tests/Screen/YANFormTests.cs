using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using YANF.Tests.Script;
using static System.Windows.Forms.FormWindowState;
using static System.Windows.Forms.MouseButtons;

namespace YANF.Tests.Screen
{
    using Control = System.Windows.Forms.Control;
    using YANPaint = YANF.Control.YANPaint;
    using WinScreen = System.Windows.Forms.Screen;

    // YANForm (2.0): a borderless base form with rounded corners (DWM on Windows 11, a region elsewhere), a drop shadow, resize edges,
    // a caption band and caption controls, and maximizing that keeps the taskbar visible. The native parts that need Windows (DWM, the move
    // loop) are not run here; mono has no dwmapi, which exercises the fallback
    public class YANFormTests
    {
        #region Fields
        private const int WM_GETMINMAXINFO = 0x24;
        private const int WM_WINDOWPOSCHANGING = 0x46;
        private const int WM_NCHITTEST = 0x84;
        private const int WM_NCLBUTTONDBLCLK = 0xA3;
        private const int WM_DPICHANGED = 0x2E0;
        private const int S_OK = 0;
        private const int E_INVALIDARG = unchecked((int)0x80070057);
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
        private const int SWP_NOZORDER = 0x4;
        private const int SWP_NOACTIVATE = 0x10;
        private static readonly Rectangle BOUNDS = new(100, 100, 300, 200);
        #endregion

        #region Defaults and designer metadata
        // A new YANForm is borderless with every chrome feature on (the caption band excepted), and the designer writes none of it
        [Fact]
        public void Defaults_AreTheDesignerDefaults_AndAreNotSerialized() => Sta.Run(() =>
        {
            using var frm = new YANForm();
            Assert.Equal(FormBorderStyle.None, frm.FormBorderStyle);
            Assert.Equal(FormBorderStyle.None, ((Form)frm).FormBorderStyle);
            Assert.True(frm.RoundedCorners);
            Assert.True(frm.DropShadow);
            Assert.True(frm.Resizable);
            Assert.Equal(8, frm.CornerRadius);
            Assert.Equal(6, frm.ResizeBorderWidth);
            Assert.Equal(0, frm.CaptionHeight);
            var props = TypeDescriptor.GetProperties(frm);
            foreach (var name in new[] { "FormBorderStyle", "RoundedCorners", "CornerRadius", "DropShadow", "Resizable", "ResizeBorderWidth", "CaptionHeight" })
            {
                var p = props[name];
                var dv = Assert.IsType<DefaultValueAttribute>(p.Attributes[typeof(DefaultValueAttribute)]);
                Assert.Equal(dv.Value, p.GetValue(frm));
                Assert.False(p.ShouldSerializeValue(frm), name + " would be written into Designer.cs for a new form");
                Assert.True(p.IsBrowsable, name);
                Assert.False(string.IsNullOrWhiteSpace(p.Description), name);
            }
            Assert.Equal("Appearance", props["FormBorderStyle"].Category);
            foreach (var name in new[] { "RoundedCorners", "CornerRadius", "DropShadow" })
            {
                Assert.Equal("YAN Appearance", props[name].Category);
            }
            foreach (var name in new[] { "Resizable", "ResizeBorderWidth", "CaptionHeight" })
            {
                Assert.Equal("YAN Behavior", props[name].Category);
            }
            // a value picked in the designer is written, the Form default (Sizable) included
            frm.FormBorderStyle = FormBorderStyle.Sizable;
            Assert.True(props["FormBorderStyle"].ShouldSerializeValue(frm));
            frm.CaptionHeight = 32;
            Assert.True(props["CaptionHeight"].ShouldSerializeValue(frm));
        });

        [Fact]
        public void NegativeSizes_AreStoredAsZero() => Sta.Run(() =>
        {
            using var frm = new YANForm
            {
                CornerRadius = -3,
                ResizeBorderWidth = -1,
                CaptionHeight = int.MinValue
            };
            Assert.Equal(0, frm.CornerRadius);
            Assert.Equal(0, frm.ResizeBorderWidth);
            Assert.Equal(0, frm.CaptionHeight);
        });
        #endregion

        #region CreateParams
        // CS_DROPSHADOW where Windows does not round (and shadow) the window itself, and only for a borderless top-level form
        [Fact]
        public void CreateParams_ClassShadow_OnlyWhereWindowsDoesNotRoundTheWindow() => Sta.Run(() =>
        {
            try
            {
                YANForm.SystemCornersOverride = false;
                using var frm = new Probe();
                Assert.True(HasClassShadow(frm));
                frm.DropShadow = false;
                Assert.False(HasClassShadow(frm));
                frm.DropShadow = true;
                frm.FormBorderStyle = FormBorderStyle.Sizable;
                Assert.False(HasClassShadow(frm));
                frm.FormBorderStyle = FormBorderStyle.None;
                frm.TopLevel = false;
                Assert.False(HasClassShadow(frm));
                frm.TopLevel = true;
                Assert.True(HasClassShadow(frm));
                // Windows 11 rounds the window and gives it the system shadow
                YANForm.SystemCornersOverride = true;
                Assert.False(HasClassShadow(frm));
                frm.RoundedCorners = false;
                Assert.True(HasClassShadow(frm));
                frm.RoundedCorners = true;
                frm.CornerRadius = 0;
                Assert.True(HasClassShadow(frm));
            }
            finally
            {
                YANForm.SystemCornersOverride = null;
            }
        });

        // A borderless Form has no system menu and no minimize/maximize styles (no taskbar minimize, no Win+Arrow): YANForm adds the ones
        // that ControlBox, MinimizeBox and MaximizeBox ask for
        [Fact]
        public void CreateParams_BorderlessWindow_GetsTheSystemMenuAndBoxStyles() => Sta.Run(() =>
        {
            using var frm = new Probe();
            Assert.Equal(WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX, frm.Params.Style & (WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX));
            frm.MinimizeBox = false;
            Assert.Equal(WS_SYSMENU | WS_MAXIMIZEBOX, frm.Params.Style & (WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX));
            frm.MaximizeBox = false;
            frm.ControlBox = false;
            Assert.Equal(0, frm.Params.Style & (WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX));
        });

        // The class style belongs to the window class: an existing window gets a new handle when it changes, and only then
        [Fact]
        public void ShadowChange_RecreatesTheHandle_OnlyWhenTheClassStyleChanges() => Sta.Run(ui =>
        {
            try
            {
                YANForm.SystemCornersOverride = false;
                using var frm = ShowProbe(ui);
                var destroyed = 0;
                frm.HandleDestroyed += (_, _) => destroyed++;
                var handle = frm.Handle;
                frm.DropShadow = true;
                frm.CaptionHeight = 20;
                frm.Resizable = false;
                frm.CornerRadius = 12;
                Assert.Equal(0, destroyed);
                frm.DropShadow = false;
                Assert.Equal(1, destroyed);
                Assert.True(frm.IsHandleCreated);
                Assert.NotEqual(handle, frm.Handle);
                Assert.False(HasClassShadow(frm));
                // rounded by Windows 11: the class shadow is off whatever DropShadow says, so no new handle is needed
                YANForm.SystemCornersOverride = true;
                frm.DropShadow = true;
                Assert.Equal(1, destroyed);
                // square corners on Windows 11: the class shadow comes back
                frm.RoundedCorners = false;
                Assert.Equal(2, destroyed);
                Assert.True(HasClassShadow(frm));
                ui.Pump(2);
            }
            finally
            {
                YANForm.SystemCornersOverride = null;
            }
        });

        // The chrome follows the border style however it is set: through a Form reference (a helper taking a Form, reflection on Form) the
        // new property of YANForm is not called, the style change is what counts
        [Fact]
        public void BorderStyleSetThroughAForm_SyncsTheClassShadowAndTheCorners() => Sta.Run(ui =>
        {
            try
            {
                YANForm.SystemCornersOverride = false;
                using var frm = ShowProbe(ui);
                Form form = frm;
                var destroyed = 0;
                frm.HandleDestroyed += (_, _) => destroyed++;
                AssertRounded(frm, 8);
                form.FormBorderStyle = FormBorderStyle.Sizable;
                Assert.Equal(1, destroyed);
                Assert.True(frm.IsHandleCreated);
                Assert.False(HasClassShadow(frm));
                Assert.Null(frm.Region);
                // a style change that keeps the class style needs no new handle
                form.MaximizeBox = false;
                Assert.Equal(1, destroyed);
                typeof(Form).GetProperty(nameof(Form.FormBorderStyle)).SetValue(frm, FormBorderStyle.None);
                Assert.Equal(2, destroyed);
                Assert.True(HasClassShadow(frm));
                AssertRounded(frm, 8);
                ui.Pump(2);
            }
            finally
            {
                YANForm.SystemCornersOverride = null;
            }
        });
        #endregion

        #region Hit test
        // The edges and corners resize the window, the caption band moves it, the rest is client area
        [Fact]
        public void NcHitTest_EdgesCornersAndCaptionBand() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui);
            var b = frm.Bounds;
            int cx = b.Left + b.Width / 2, cy = b.Top + b.Height / 2;
            Assert.Equal(HTLEFT, frm.HitTest(b.Left, cy));
            Assert.Equal(HTLEFT, frm.HitTest(b.Left + 5, cy));
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 6, cy));
            Assert.Equal(HTRIGHT, frm.HitTest(b.Right - 1, cy));
            Assert.Equal(HTRIGHT, frm.HitTest(b.Right - 6, cy));
            Assert.Equal(HTCLIENT, frm.HitTest(b.Right - 7, cy));
            Assert.Equal(HTTOP, frm.HitTest(cx, b.Top + 2));
            Assert.Equal(HTBOTTOM, frm.HitTest(cx, b.Bottom - 2));
            Assert.Equal(HTTOPLEFT, frm.HitTest(b.Left + 1, b.Top + 1));
            Assert.Equal(HTTOPRIGHT, frm.HitTest(b.Right - 2, b.Top + 1));
            Assert.Equal(HTBOTTOMLEFT, frm.HitTest(b.Left + 1, b.Bottom - 1));
            Assert.Equal(HTBOTTOMRIGHT, frm.HitTest(b.Right - 1, b.Bottom - 1));
            Assert.Equal(HTCLIENT, frm.HitTest(cx, cy));
            Assert.Equal(HTCLIENT, frm.HitTest(cx, b.Top + 20));
            // the caption band: below the top edge, it moves the window; the edge still resizes
            frm.CaptionHeight = 30;
            Assert.Equal(HTCAPTION, frm.HitTest(cx, b.Top + 20));
            Assert.Equal(HTCAPTION, frm.HitTest(cx, b.Top + 29));
            Assert.Equal(HTCLIENT, frm.HitTest(cx, b.Top + 30));
            Assert.Equal(HTTOP, frm.HitTest(cx, b.Top + 2));
            Assert.Equal(HTLEFT, frm.HitTest(b.Left + 1, b.Top + 20));
            // no edges without Resizable or with a zero width: the band reaches the top edge then
            frm.Resizable = false;
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 1, cy));
            Assert.Equal(HTCAPTION, frm.HitTest(cx, b.Top + 1));
            Assert.Equal(HTCAPTION, frm.HitTest(b.Left, b.Top));
            frm.Resizable = true;
            frm.ResizeBorderWidth = 0;
            Assert.Equal(HTCLIENT, frm.HitTest(b.Right - 1, cy));
            Assert.Equal(HTCAPTION, frm.HitTest(cx, b.Top));
            frm.ResizeBorderWidth = 10;
            Assert.Equal(HTRIGHT, frm.HitTest(b.Right - 10, cy));
        });

        // A direction the size cannot change in has no edge (MinimumSize equal to MaximumSize, or AutoSize with GrowAndShrink)
        [Fact]
        public void NcHitTest_NoEdges_InAFixedDirection() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui, f =>
            {
                f.MinimumSize = new Size(300, 100);
                f.MaximumSize = new Size(300, 400);
            });
            var b = frm.Bounds;
            int cx = b.Left + b.Width / 2, cy = b.Top + b.Height / 2;
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 1, cy));
            Assert.Equal(HTCLIENT, frm.HitTest(b.Right - 1, cy));
            Assert.Equal(HTTOP, frm.HitTest(b.Left + 1, b.Top + 1));
            Assert.Equal(HTBOTTOM, frm.HitTest(b.Right - 1, b.Bottom - 1));
            frm.MaximumSize = Size.Empty;
            Assert.Equal(HTLEFT, frm.HitTest(b.Left + 1, cy));
            // GrowAndShrink: the form sizes itself
            frm.AutoSize = true;
            frm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b = frm.Bounds;
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 1, b.Top + b.Height / 2));
        });

        [Fact]
        public void NcHitTest_NoEdges_WhileMaximized() => Sta.Run(() =>
        {
            // a hidden window: WindowState is only stored, the bounds stay
            using var frm = new Probe
            {
                CaptionHeight = 30
            };
            _ = frm.Handle;
            var b = frm.Bounds;
            frm.WindowState = Maximized;
            Assert.Equal(Maximized, frm.WindowState);
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 1, b.Top + 50));
            Assert.Equal(HTCLIENT, frm.HitTest(b.Right - 1, b.Bottom - 1));
            // the band still acts as the caption (its double-click restores the window), up to the top edge
            Assert.Equal(HTCAPTION, frm.HitTest(b.Left + 1, b.Top + 1));
        });

        // The band widths are 96-dpi logical pixels
        [Fact]
        public void NcHitTest_ScalesWithTheDpi() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui, f => f.CaptionHeight = 20);
            var b = frm.Bounds;
            var cy = b.Top + b.Height / 2;
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 7, cy));
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 150, b.Top + 25));
            try
            {
                YANPaint.DpiOverride = 144;
                // 6 * 1.5 = 9 and 20 * 1.5 = 30
                Assert.Equal(HTLEFT, frm.HitTest(b.Left + 7, cy));
                Assert.Equal(HTLEFT, frm.HitTest(b.Left + 8, cy));
                Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 9, cy));
                Assert.Equal(HTCAPTION, frm.HitTest(b.Left + 150, b.Top + 25));
                Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 150, b.Top + 30));
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // With a border the chrome is Windows' own, and an answer of Windows other than HTCLIENT is kept
        [Fact]
        public void NcHitTest_FramedForm_OrOtherSystemAnswers_AreLeftToWindows() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui, f => f.CaptionHeight = 30);
            var b = frm.Bounds;
            Assert.Equal(0, frm.ChromeHitTest(0, new Point(b.Left + 1, b.Top + 1)));
            Assert.Equal(HTBOTTOMRIGHT, frm.ChromeHitTest(HTBOTTOMRIGHT, new Point(b.Left + 50, b.Top + 50)));
            // a point outside the window
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left - 1, b.Top + 50));
            frm.FormBorderStyle = FormBorderStyle.FixedSingle;
            var client = frm.RectangleToScreen(frm.ClientRectangle);
            Assert.Equal(HTCLIENT, frm.HitTest(client.Left + 1, client.Top + client.Height / 2));
            Assert.Equal(HTCLIENT, frm.HitTest(client.Left + client.Width / 2, client.Top + 10));
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.TopLevel = false;
            Assert.Equal(HTCLIENT, frm.HitTest(b.Left + 1, b.Top + 50));
        });

        // WM_NCHITTEST through the window procedure: Windows answers HTCLIENT for the client area of a borderless window, and YANForm
        // turns it into the edges and the caption band (mono's DefWndProc answers 0 here: see the scratch skip list)
        [Fact]
        public void WmNcHitTest_RefinesTheSystemAnswer() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui, f => f.CaptionHeight = 30);
            var b = frm.Bounds;
            int cx = b.Left + b.Width / 2, cy = b.Top + b.Height / 2;
            Assert.Equal(HTCLIENT, frm.WmHitTest(cx, cy));
            Assert.Equal(HTLEFT, frm.WmHitTest(b.Left + 1, cy));
            Assert.Equal(HTBOTTOMRIGHT, frm.WmHitTest(b.Right - 1, b.Bottom - 1));
            Assert.Equal(HTCAPTION, frm.WmHitTest(cx, b.Top + 20));
            frm.FormBorderStyle = FormBorderStyle.FixedSingle;
            var client = frm.RectangleToScreen(frm.ClientRectangle);
            Assert.Equal(HTCLIENT, frm.WmHitTest(client.Left + client.Width / 2, client.Top + 10));
        });
        #endregion

        #region Maximizing
        // WM_GETMINMAXINFO: the maximized bounds of a borderless window are the working area of its monitor (it covered the taskbar)
        [Fact]
        public void GetMinMaxInfo_MaximizesToTheWorkingArea() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui);
            var screen = WinScreen.FromHandle(frm.Handle);
            // the working area, 2 px shorter along an auto-hide taskbar of this machine (none here: mono has no shell32)
            var area = YANForm.GetMaximizedArea(screen);
            var work = screen.WorkingArea;
            Assert.True(work.Contains(area), $"maximized area {area}, working area {work}");
            Assert.InRange(area.Left - work.Left + area.Top - work.Top + work.Right - area.Right + work.Bottom - area.Bottom, 0, 8);
            var expected = YANForm.GetMaximizedBounds(screen.Bounds, area);
            var mmi = frm.SendMinMaxInfo(screen.Bounds);
            Assert.Equal(expected, frm.MaxBounds);
            Assert.Equal(expected.Location, new Point(mmi.ptMaxPosition.X, mmi.ptMaxPosition.Y));
            Assert.Equal(expected.Size, new Size(mmi.ptMaxSize.X, mmi.ptMaxSize.Y));
            if (screen.Primary)
            {
                Assert.Equal(area, frm.MaxBounds);
            }
        });

        // MaximizedBounds set by the derived form wins; with a border Windows keeps the taskbar visible itself (its defaults stay)
        [Fact]
        public void GetMinMaxInfo_KeepsTheDerivedFormsBounds_AndLeavesFramedFormsAlone() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui);
            var monitor = WinScreen.FromHandle(frm.Handle).Bounds;
            _ = frm.SendMinMaxInfo(monitor);
            Assert.False(frm.MaxBounds.IsEmpty);
            frm.FormBorderStyle = FormBorderStyle.Sizable;
            var mmi = frm.SendMinMaxInfo(monitor);
            Assert.Equal(new Size(monitor.Width + 16, monitor.Height + 16), new Size(mmi.ptMaxSize.X, mmi.ptMaxSize.Y));
            Assert.Equal(new Point(-8, -8), new Point(mmi.ptMaxPosition.X, mmi.ptMaxPosition.Y));
            frm.FormBorderStyle = FormBorderStyle.None;
            var own = new Rectangle(10, 20, 500, 400);
            frm.MaxBounds = own;
            mmi = frm.SendMinMaxInfo(monitor);
            Assert.Equal(own, frm.MaxBounds);
            Assert.Equal(own.Size, new Size(mmi.ptMaxSize.X, mmi.ptMaxSize.Y));
            Assert.Equal(own.Location, new Point(mmi.ptMaxPosition.X, mmi.ptMaxPosition.Y));
            frm.FormBorderStyle = FormBorderStyle.Sizable;
            _ = frm.SendMinMaxInfo(monitor);
            Assert.Equal(own, frm.MaxBounds);
        });

        // Windows reads MINMAXINFO.ptMaxPosition for the primary monitor and moves it to the monitor the window maximizes on
        [Fact]
        public void GetMaximizedBounds_IsRelativeToTheMonitor()
        {
            Assert.Equal(new Rectangle(0, 0, 1920, 1040), YANForm.GetMaximizedBounds(new Rectangle(0, 0, 1920, 1080), new Rectangle(0, 0, 1920, 1040)));
            // a secondary monitor on the right, the taskbar at the bottom
            Assert.Equal(new Rectangle(0, 0, 2560, 1400), YANForm.GetMaximizedBounds(new Rectangle(1920, 0, 2560, 1440), new Rectangle(1920, 0, 2560, 1400)));
            // a monitor on the left, the taskbar on its left side
            Assert.Equal(new Rectangle(62, 0, 1218, 1024), YANForm.GetMaximizedBounds(new Rectangle(-1280, -200, 1280, 1024), new Rectangle(-1218, -200, 1218, 1024)));
            // the taskbar at the top of the primary monitor
            Assert.Equal(new Rectangle(0, 48, 1920, 1032), YANForm.GetMaximizedBounds(new Rectangle(0, 0, 1920, 1080), new Rectangle(0, 48, 1920, 1032)));
        }

        // With an auto-hide taskbar the working area is the whole monitor, and a window that covers the whole monitor is taken for a
        // full-screen one (the hidden taskbar no longer comes up): the maximized window stops 2 px short of each auto-hide edge
        [Fact]
        public void GetMaximizedBounds_StopsShortOfAnAutoHideTaskbar()
        {
            var primary = new Rectangle(0, 0, 1920, 1080);
            Assert.Equal(new Rectangle(0, 0, 1920, 1078), YANForm.GetMaximizedBounds(primary, primary, AnchorStyles.Bottom));
            Assert.Equal(new Rectangle(0, 2, 1920, 1078), YANForm.GetMaximizedBounds(primary, primary, AnchorStyles.Top));
            Assert.Equal(new Rectangle(2, 0, 1918, 1080), YANForm.GetMaximizedBounds(primary, primary, AnchorStyles.Left));
            Assert.Equal(new Rectangle(0, 0, 1918, 1080), YANForm.GetMaximizedBounds(primary, primary, AnchorStyles.Right));
            Assert.Equal(new Rectangle(2, 2, 1916, 1076), YANForm.GetMaximizedBounds(primary, primary, AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right));
            Assert.Equal(primary, YANForm.GetMaximizedBounds(primary, primary, AnchorStyles.None));
            // a secondary monitor: relative to the monitor, as without the taskbar
            var secondary = new Rectangle(-2560, -360, 2560, 1440);
            Assert.Equal(new Rectangle(0, 0, 2560, 1438), YANForm.GetMaximizedBounds(secondary, secondary, AnchorStyles.Bottom));
            Assert.Equal(new Rectangle(-2560, -360, 2560, 1438), YANForm.GetMaximizedArea(secondary, secondary, AnchorStyles.Bottom));
            // an edge that the working area does not reach (another bar is docked there) keeps the working area
            var docked = new Rectangle(0, 0, 1920, 1040);
            Assert.Equal(docked, YANForm.GetMaximizedBounds(primary, docked, AnchorStyles.Bottom));
            Assert.Equal(new Rectangle(2, 0, 1918, 1040), YANForm.GetMaximizedBounds(primary, docked, AnchorStyles.Bottom | AnchorStyles.Left));
        }

        // A maximized borderless window fits the working area
        [Fact]
        public void Maximize_KeepsTheTaskbarVisible() => Sta.Run(ui =>
        {
            using var frm = ShowProbe(ui);
            var work = WinScreen.FromHandle(frm.Handle).WorkingArea;
            frm.WindowState = Maximized;
            ui.Pump();
            Assert.Equal(Maximized, frm.WindowState);
            Assert.False(frm.MaxBounds.IsEmpty);
            Assert.True(work.Contains(frm.Bounds), $"maximized to {frm.Bounds}, working area {work}");
            frm.WindowState = Normal;
            ui.Pump();
            Assert.Equal(BOUNDS.Size, frm.Size);
        });

        // A maximized size that Windows enlarged (a monitor larger than the primary one) is brought back inside the working area
        [Fact]
        public void WindowPosChanging_KeepsTheMaximizedWindowInsideTheWorkingArea() => Sta.Run(ui =>
        {
            using var shown = ShowProbe(ui);
            shown.WindowState = Maximized;
            ui.Pump();
            using var hidden = new Probe();
            var frm = shown;
            if (shown.WindowState != Maximized)
            {
                // mono under xvfb (no window manager) does not maximize a shown form; a form that was never shown keeps the WindowState it is
                // given, which YANForm goes by where user32!IsZoomed cannot be called
                _ = hidden.Handle;
                hidden.WindowState = Maximized;
                frm = hidden;
            }
            var screen = WinScreen.FromHandle(frm.Handle);
            var work = YANForm.GetMaximizedArea(screen);
            _ = frm.SendMinMaxInfo(screen.Bounds);
            var tooLarge = new Rectangle(work.Location, work.Size + new Size(4, 4));
            Assert.Equal(work, frm.SendWindowPosChanging(tooLarge));
            // a size inside the working area is kept
            var smaller = new Rectangle(work.X + 10, work.Y + 10, work.Width / 2, work.Height / 2);
            Assert.Equal(smaller, frm.SendWindowPosChanging(smaller));
            // not maximized: the working area does not limit the window
            frm.WindowState = Normal;
            ui.Pump();
            var normal = frm.SendWindowPosChanging(tooLarge);
            Assert.True(normal.Width > work.Width || normal.Height > work.Height, $"a normal window was fitted to the working area: {normal}");
        });
        #endregion

        #region Caption
        // A double-click on the caption maximizes and restores a borderless window (Windows only does it for a window with a caption)
        [Fact]
        public void CaptionDoubleClick_MaximizesAndRestores() => Sta.Run(() =>
        {
            // a hidden window: WindowState is only stored
            using var frm = new Probe();
            _ = frm.Handle;
            frm.Send(WM_NCLBUTTONDBLCLK, (IntPtr)HTCAPTION, IntPtr.Zero);
            Assert.Equal(Maximized, frm.WindowState);
            frm.Send(WM_NCLBUTTONDBLCLK, (IntPtr)HTCAPTION, IntPtr.Zero);
            Assert.Equal(Normal, frm.WindowState);
            frm.MaximizeBox = false;
            frm.Send(WM_NCLBUTTONDBLCLK, (IntPtr)HTCAPTION, IntPtr.Zero);
            Assert.Equal(Normal, frm.WindowState);
        });

        [Fact]
        public void RegisterCaptionControl_SubscribesOnce_AndForgetsDisposedControls() => Sta.Run(() =>
        {
            using var frm = new YANForm();
            var pnl = new Panel();
            frm.Controls.Add(pnl);
            frm.RegisterCaptionControl(pnl);
            frm.RegisterCaptionControl(pnl);
            Assert.True(frm.IsCaptionControl(pnl));
            Assert.Equal(1, Handlers.Count(pnl, nameof(Control.MouseDown)));
            frm.UnregisterCaptionControl(pnl);
            frm.UnregisterCaptionControl(pnl);
            Assert.False(frm.IsCaptionControl(pnl));
            Assert.Equal(0, Handlers.Count(pnl, nameof(Control.MouseDown)));
            // a disposed control is forgotten, and a control that outlives the form is released with it
            frm.RegisterCaptionControl(pnl);
            pnl.Dispose();
            Assert.False(frm.IsCaptionControl(pnl));
            frm.RegisterCaptionControl(pnl);
            Assert.False(frm.IsCaptionControl(pnl));
            using var outside = new Label();
            frm.RegisterCaptionControl(outside);
            frm.Dispose();
            Assert.Equal(0, Handlers.Count(outside, nameof(Control.MouseDown)));
            Assert.Throws<ArgumentNullException>(() => frm.RegisterCaptionControl(null));
            Assert.Throws<ArgumentNullException>(() => frm.UnregisterCaptionControl(null));
        });

        // Only the left button acts on a caption control; its double-click maximizes and restores the form (the single press starts the
        // Windows move loop, which is not run here)
        [Fact]
        public void CaptionControl_DoubleClick_MaximizesAndRestores_OtherButtonsDoNothing() => Sta.Run(() =>
        {
            using var frm = new Probe();
            var pnl = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30
            };
            frm.Controls.Add(pnl);
            _ = frm.Handle;
            frm.RegisterCaptionControl(pnl);
            Priv.Call(pnl, "OnMouseDown", new MouseEventArgs(Right, 2, 5, 5, 0));
            Priv.Call(pnl, "OnMouseDown", new MouseEventArgs(Middle, 2, 5, 5, 0));
            Assert.Equal(Normal, frm.WindowState);
            Priv.Call(pnl, "OnMouseDown", new MouseEventArgs(Left, 2, 5, 5, 0));
            Assert.Equal(Maximized, frm.WindowState);
            Priv.Call(pnl, "OnMouseDown", new MouseEventArgs(Left, 2, 5, 5, 0));
            Assert.Equal(Normal, frm.WindowState);
        });
        #endregion

        #region Corners
        // Where Windows does not round the window, a region of CornerRadius does, rebuilt for each size; square corners remove it
        [Fact]
        public void RegionCorners_WhereWindowsDoesNotRound() => Sta.Run(ui =>
        {
            try
            {
                YANForm.SystemCornersOverride = false;
                using var frm = ShowProbe(ui);
                Assert.False(frm.IsSystemRounded);
                AssertRounded(frm, 8);
                frm.Size = new Size(400, 250);
                ui.Pump(2);
                AssertRounded(frm, 8);
                frm.CornerRadius = 20;
                AssertRounded(frm, 20);
                frm.RoundedCorners = false;
                Assert.Null(frm.Region);
                frm.RoundedCorners = true;
                AssertRounded(frm, 20);
                frm.CornerRadius = 0;
                Assert.Null(frm.Region);
                // a region of the derived form stays while the corners are square
                using var own = new Region(new Rectangle(0, 0, 50, 50));
                frm.Region = own;
                frm.Size = new Size(380, 240);
                ui.Pump(2);
                Assert.Same(own, frm.Region);
                frm.Region = null;
                // with a border the window is Windows' own
                frm.CornerRadius = 8;
                AssertRounded(frm, 8);
                frm.FormBorderStyle = FormBorderStyle.FixedSingle;
                Assert.Null(frm.Region);
            }
            finally
            {
                YANForm.SystemCornersOverride = null;
            }
        });

        [Fact]
        public void RegionCorners_ScaleWithTheDpi() => Sta.Run(ui =>
        {
            try
            {
                YANForm.SystemCornersOverride = false;
                using var frm = ShowProbe(ui);
                Assert.True(frm.Region.IsVisible(3, 3));
                YANPaint.DpiOverride = 192;
                frm.Size = new Size(320, 220);
                ui.Pump(2);
                // 8 * 2 = 16
                AssertRounded(frm, 16);
                Assert.False(frm.Region.IsVisible(3, 3));
            }
            finally
            {
                YANPaint.DpiOverride = null;
                YANForm.SystemCornersOverride = null;
            }
        });

        // WM_DPICHANGED applies the corners again, also when the size stays (the new DPI's region is not built by a size change)
        [Fact]
        public void RegionCorners_FollowWmDpiChanged_WithoutAResize() => Sta.Run(ui =>
        {
            var rect = Marshal.AllocHGlobal(Marshal.SizeOf<RECT>());
            try
            {
                YANForm.SystemCornersOverride = false;
                using var frm = ShowProbe(ui);
                AssertRounded(frm, 8);
                var b = frm.Bounds;
                Marshal.StructureToPtr(new RECT { Left = b.Left, Top = b.Top, Right = b.Right, Bottom = b.Bottom }, rect, false);
                YANPaint.DpiOverride = 192;
                _ = frm.Send(WM_DPICHANGED, (IntPtr)((192 << 16) | 192), rect);
                Assert.Equal(b.Size, frm.Size);
                // 8 * 2 = 16
                AssertRounded(frm, 16);
            }
            finally
            {
                Marshal.FreeHGlobal(rect);
                YANPaint.DpiOverride = null;
                YANForm.SystemCornersOverride = null;
            }
        });

        // A maximized window has square corners, and gets its rounded ones back when it is restored
        [Fact]
        public void RegionCorners_SquareWhileMaximized() => Sta.Run(() =>
        {
            try
            {
                YANForm.SystemCornersOverride = false;
                using var frm = new Probe();
                _ = frm.Handle;
                AssertRounded(frm, 8);
                frm.WindowState = Maximized;
                frm.CornerRadius = 10;
                Assert.Null(frm.Region);
                frm.WindowState = Normal;
                frm.CornerRadius = 8;
                AssertRounded(frm, 8);
            }
            finally
            {
                YANForm.SystemCornersOverride = null;
            }
        });

        // As if on Windows 11 where DWM cannot be called (mono has no dwmapi) or refuses: the region takes over, nothing throws, also for a
        // translucent (layered) window and across new handles
        [Fact]
        public void SystemCorners_Unavailable_FallBackWithoutThrowing() => Sta.Run(ui =>
        {
            try
            {
                YANForm.SystemCornersOverride = true;
                using var frm = ShowProbe(ui);
                AssertCorners(frm);
                frm.Opacity = 0.5;
                ui.Pump(2);
                AssertCorners(frm);
                frm.Size = new Size(350, 260);
                ui.Pump(2);
                AssertCorners(frm);
                frm.RoundedCorners = false;
                Assert.False(frm.IsSystemRounded);
                Assert.Null(frm.Region);
                frm.RoundedCorners = true;
                frm.Opacity = 1;
                ui.Pump(2);
                AssertCorners(frm);
                frm.DropShadow = false;
                frm.WindowState = Maximized;
                ui.Pump(2);
                frm.WindowState = Normal;
                ui.Pump(2);
                AssertCorners(frm);
                ui.ThrowIfFailed();
            }
            finally
            {
                YANForm.SystemCornersOverride = null;
            }

            static void AssertCorners(Probe frm)
            {
                if (!frm.IsSystemRounded)
                {
                    AssertRounded(frm, 8);
                }
            }
        });

        // DWM refusing the corner preference for one window (E_INVALIDARG) concerns that window only: it falls back to the region and asks
        // again at its next size change, and other windows are still rounded by Windows
        [Fact]
        public void SystemCorners_RefusedForOneWindow_DoesNotTurnThemOffForOthers() => Sta.Run(ui =>
        {
            try
            {
                YANForm.SystemCornersOverride = true;
                var refuse = true;
                var calls = 0;
                YANForm.CornerPreferenceOverride = (_, _) =>
                {
                    calls++;
                    return refuse ? E_INVALIDARG : S_OK;
                };
                using var first = new Probe();
                _ = first.Handle;
                Assert.True(calls > 0);
                Assert.False(first.IsSystemRounded);
                AssertRounded(first, 8);
                // Windows 11 would round the window: no class shadow, whatever the refusal
                Assert.False(HasClassShadow(first));
                // the refusal is not remembered for the next window
                refuse = false;
                using var second = new Probe();
                _ = second.Handle;
                Assert.True(second.IsSystemRounded);
                Assert.Null(second.Region);
                // the refused window asks again at its next size change, and DWM rounds it now
                calls = 0;
                first.Size = new Size(320, 240);
                Assert.Equal(1, calls);
                Assert.True(first.IsSystemRounded);
                Assert.Null(first.Region);
                // accepted: not asked again for the same handle
                first.Size = new Size(330, 250);
                Assert.Equal(1, calls);
                ui.ThrowIfFailed();
            }
            finally
            {
                YANForm.CornerPreferenceOverride = null;
                YANForm.SystemCornersOverride = null;
            }
        });

        // The corner region belongs to the form, which Control.Dispose does not free: YANForm frees it once the window is gone
        [Fact]
        public void Dispose_FreesTheCornerRegion() => Sta.Run(ui =>
        {
            try
            {
                YANForm.SystemCornersOverride = false;
                var frm = ShowProbe(ui);
                var region = frm.Region;
                AssertRounded(frm, 8);
                frm.Dispose();
                Assert.Throws<ArgumentException>(() => region.IsVisible(0, 0));
                ui.ThrowIfFailed();
            }
            finally
            {
                YANForm.SystemCornersOverride = null;
            }
        });
        #endregion

        #region Helpers
        private static Probe ShowProbe(Ui ui, Action<Probe> setup = null)
        {
            var frm = new Probe();
            try
            {
                setup?.Invoke(frm);
                frm.Show();
                ui.Pump(3);
                return frm;
            }
            catch
            {
                frm.Dispose();
                throw;
            }
        }

        private static bool HasClassShadow(Probe frm) => (frm.Params.ClassStyle & CS_DROPSHADOW) != 0;

        // The form's region is the rounded rectangle of the window with this device radius
        private static void AssertRounded(Form frm, int radius)
        {
            var region = frm.Region;
            Assert.NotNull(region);
            int w = frm.Width, h = frm.Height;
            // the corner pixels are cut, the edges between the corners and the centre are kept (the right and bottom edges included)
            foreach (var p in new[] { new Point(0, 0), new Point(w - 1, 0), new Point(0, h - 1), new Point(w - 1, h - 1) })
            {
                Assert.False(region.IsVisible(p), $"corner {p} of {w}x{h} is inside the region");
            }
            foreach (var p in new[] { new Point(w / 2, 0), new Point(w / 2, h - 1), new Point(0, h / 2), new Point(w - 1, h / 2), new Point(w / 2, h / 2) })
            {
                Assert.True(region.IsVisible(p), $"{p} of {w}x{h} is outside the region");
            }
            // the arc: a point just inside the corner square but outside the circle is cut, the circle's centre is kept
            var off = Math.Max(0, (int)Math.Floor(radius * (1 - Math.Sqrt(0.5))) - 1);
            Assert.False(region.IsVisible(off, off), $"({off},{off}) is inside a corner of radius {radius}");
            Assert.True(region.IsVisible(radius, radius));
        }
        #endregion

        #region Nested types
        private sealed class Probe : YANForm
        {
            public Probe()
            {
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Bounds = BOUNDS;
            }

            public CreateParams Params => CreateParams;

            public Rectangle MaxBounds
            {
                get => MaximizedBounds;
                set => MaximizedBounds = value;
            }

            public IntPtr Send(int msg, IntPtr wParam, IntPtr lParam)
            {
                var m = Message.Create(Handle, msg, wParam, lParam);
                WndProc(ref m);
                return m.Result;
            }

            // The chrome's answer where Windows answers HTCLIENT (the client area of a borderless window)
            public int HitTest(int x, int y) => ChromeHitTest(HTCLIENT, new Point(x, y));

            // WM_NCHITTEST through the window procedure
            public int WmHitTest(int x, int y) => (int)Send(WM_NCHITTEST, IntPtr.Zero, (IntPtr)unchecked((y << 16) | (x & 0xFFFF))).ToInt64();

            // WM_GETMINMAXINFO with the defaults of Windows for a sizable window on the monitor (maximized over its edges by the frame width)
            public MINMAXINFO SendMinMaxInfo(Rectangle monitor)
            {
                var mmi = new MINMAXINFO
                {
                    ptMaxSize = new POINT(monitor.Width + 16, monitor.Height + 16),
                    ptMaxPosition = new POINT(-8, -8),
                    ptMinTrackSize = new POINT(1, 1),
                    ptMaxTrackSize = new POINT(monitor.Width + 16, monitor.Height + 16)
                };
                var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<MINMAXINFO>());
                try
                {
                    Marshal.StructureToPtr(mmi, ptr, false);
                    _ = Send(WM_GETMINMAXINFO, IntPtr.Zero, ptr);
                    return Marshal.PtrToStructure<MINMAXINFO>(ptr);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }

            // WM_WINDOWPOSCHANGING that moves and sizes the window to the rectangle; returns the rectangle after the form handled it
            public Rectangle SendWindowPosChanging(Rectangle rect)
            {
                var pos = new WINDOWPOS
                {
                    hwnd = Handle,
                    x = rect.X,
                    y = rect.Y,
                    cx = rect.Width,
                    cy = rect.Height,
                    flags = SWP_NOZORDER | SWP_NOACTIVATE
                };
                var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<WINDOWPOS>());
                try
                {
                    Marshal.StructureToPtr(pos, ptr, false);
                    _ = Send(WM_WINDOWPOSCHANGING, IntPtr.Zero, ptr);
                    pos = Marshal.PtrToStructure<WINDOWPOS>(ptr);
                    return new Rectangle(pos.x, pos.y, pos.cx, pos.cy);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }
        #endregion
    }
}
