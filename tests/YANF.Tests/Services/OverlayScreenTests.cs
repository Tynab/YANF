using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using YANF.Script;
using static YANF.Tests.Services.OverlayKit;

namespace YANF.Tests.Services
{
    using Control = System.Windows.Forms.Control;
    using YANPaint = YANF.Control.YANPaint;

    public class OverlayScreenTests
    {
        // The legacy services' constructors place the screen over the snapshot, the round region is set from the corner, and a second
        // close is a no-op
        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        public void LegacyConstructors_AndCloseAfterDispose(int corner) => Sta.Run(() =>
        {
            using var p = NewParent();
            var load = new YANLoadScreen(p.Bounds, corner, false);
            Assert.Equal(p.Bounds, load.Bounds);
            Assert.Equal(corner > 0, load.Region != null);
            Assert.False(load.TopMost);
            load.Dispose();
            // already disposed: no-op
            load.Frm_Close();
            var wait = new YANWaitScreen(p.Bounds, corner, true);
            Assert.Equal(p.Bounds, wait.Bounds);
            Assert.Equal(corner > 0, wait.Region != null);
            Assert.True(wait.TopMost);
            wait.Dispose();
            wait.Frm_Close();
            var upd = new YANUpdateScreen();
            upd.Dispose();
            upd.Frm_Close();
        });

        // The three screens share YANOverlayScreen; Frm_Close still closes and disposes before it returns
        [Fact]
        public void Screens_ShareTheOverlayBase_AndCloseSynchronously() => Sta.Run(() =>
        {
            using var p = NewParent();
            using (var load = new YANLoadScreen(p.Bounds, 0, false))
            {
                Assert.IsAssignableFrom<YANOverlayScreen>(load);
            }
            using (var wait = new YANWaitScreen(p.Bounds, 0, false))
            {
                Assert.IsAssignableFrom<YANOverlayScreen>(wait);
            }
            var upd = new YANUpdateScreen
            {
                ShowInTaskbar = false
            };
            Assert.IsAssignableFrom<YANOverlayScreen>(upd);
            upd.Show();
            upd.Frm_Close();
            Assert.True(upd.IsDisposed, "Frm_Close returned before the screen was disposed");
            Assert.Equal(DialogResult.OK, upd.DialogResult);
            // the designer's base type: parameterless, closes too
            var bare = new YANOverlayScreen();
            bare.SetProgress(10, "ignored");
            bare.Frm_Close();
            Assert.True(bare.IsDisposed);
        });

        // 1.0.x threw NotImplementedException from MiddleScreen.Frm_Close
        [Fact]
        public void MiddleScreen_FrmClose_Closes() => Sta.Run(() =>
        {
            var shown = new MiddleScreen
            {
                ShowInTaskbar = false
            };
            shown.Show();
            shown.Frm_Close();
            Assert.False(shown.Visible);
            shown.Dispose();
            var hidden = new MiddleScreen();
            hidden.Frm_Close();
            Assert.True(hidden.IsDisposed);
        });

        // SetProgress: Load shows the percentage, Wait shows nothing, Update sizes its bar from its own width (no W_UPDATE_SCR)
        [Fact]
        public void SetProgress_PerScreen() => Sta.Run(() =>
        {
            using var p = NewParent();
            using var load = new YANLoadScreen(p.Bounds, 0, false);
            load.SetProgress(42, "not shown");
            Assert.Equal("42%", load.lblPercent.Text);
            using var wait = new YANWaitScreen(p.Bounds, 0, false);
            wait.SetProgress(42, "not shown");
            using var upd = new YANUpdateScreen();
            upd.SetProgress(50, "68.5 MB / 137 MB");
            Assert.Equal("50%", upd.lblPercent.Text);
            Assert.Equal("68.5 MB / 137 MB", upd.lblCapacity.Text);
            Assert.Equal((int)Math.Ceiling(upd.ClientSize.Width / 2d), upd.pnlProgressBar.Width);
            upd.SetProgress(250, null);
            Assert.Equal(upd.ClientSize.Width, upd.pnlProgressBar.Width);
            Assert.Equal("", upd.lblCapacity.Text);
            upd.SetProgress(-1, null);
            Assert.Equal(0, upd.pnlProgressBar.Width);
            upd.Width = 500;
            upd.SetProgress(100, null);
            Assert.Equal(upd.ClientSize.Width, upd.pnlProgressBar.Width);
        });

        // 2.0: the overlay and message box screens are internal (with their designer fields); the base forms stay public
        [Fact]
        public void Metadata_ScreensAreInternal_BaseFormsStayPublic()
        {
            foreach (var t in new[] { typeof(YANLoadScreen), typeof(YANWaitScreen), typeof(YANUpdateScreen), typeof(YANOverlayScreen), typeof(YANMessageBoxScreen) })
            {
                Assert.False(t.IsPublic, t.Name + " should be internal");
                Assert.Empty(t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static));
            }
            foreach (var t in new[] { typeof(HardScreen), typeof(SoftScreen), typeof(AnonScreen), typeof(MiddleScreen) })
            {
                Assert.True(t.IsPublic, t.Name + " should stay public");
            }
            // no public type of the library exposes an internal screen
            var screens = typeof(YANOverlayScreen).Assembly.GetTypes().Where(t => t.Namespace == "YANF.Screen" && !t.IsPublic).ToArray();
            foreach (var t in typeof(YANOverlayScreen).Assembly.GetExportedTypes())
            {
                Assert.DoesNotContain(t.BaseType, screens);
            }
        }

        // 2.0: the screens are designed at 96 dpi and scale with AutoScaleMode.Dpi, already in their constructors (before the services
        // and YANLoader place them); at 96 dpi they keep their 1.0 sizes
        [Fact]
        public void Screens_ScaleByDpi_InTheConstructor() => Sta.Run(() =>
        {
            using var load = new YANLoadScreen();
            using var wait = new YANWaitScreen();
            using var upd = new YANUpdateScreen();
            using var box = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = "Hi" });
            foreach (var scr in new Form[] { load, wait, upd, box })
            {
                Assert.Equal(AutoScaleMode.Dpi, scr.AutoScaleMode);
                // nothing left for a later layout to scale over the bounds set by the caller
                Assert.Equal(scr.CurrentAutoScaleDimensions, scr.AutoScaleDimensions);
            }
            if (upd.CurrentAutoScaleDimensions.Width == 96)
            {
                Assert.Equal(new Size(800, 600), load.ClientSize);
                Assert.Equal(new Size(800, 600), wait.ClientSize);
                Assert.Equal(new Size(YANConstant.W_UPDATE_SCR, 240), upd.ClientSize);
            }
        });

        // At 150 % the Update screen is 1.5 times as big; the legacy bar width (out of W_UPDATE_SCR) is scaled with it, the width that
        // YANLoader computes is taken from the screen itself
        [Fact]
        public void UpdateScreen_BarWidth_FollowsTheDpi() => Sta.Run(() =>
        {
            YANPaint.DpiOverride = 144;
            try
            {
                var parent = new Rectangle(100, 80, 640, 480);
                using var upd = new YANUpdateScreen(parent);
                Assert.Equal(new Size(540, 360), upd.ClientSize);
                var bar = upd.pnlProgressBar;
                upd.ShowValues(100, "137 MB", YANConstant.W_UPDATE_SCR);
                Assert.Equal(upd.ClientSize.Width, bar.Width);
                upd.ShowValues(50, "68.5 MB / 137 MB", YANConstant.W_UPDATE_SCR / 2);
                Assert.Equal(270, bar.Width);
                upd.SetProgress(40, null);
                Assert.Equal(216, bar.Width);
                // centred on the snapshot with its scaled size (inside the working area)
                var area = System.Windows.Forms.Screen.FromRectangle(parent).WorkingArea;
                var expected = new Point(
                    Math.Max(area.Left, Math.Min((parent.Left + parent.Right - upd.Width) / 2, area.Right - upd.Width)),
                    Math.Max(area.Top, Math.Min((parent.Top + parent.Bottom - upd.Height) / 2, area.Bottom - upd.Height)));
                Assert.Equal(expected, upd.Location);
                // the stacked parts keep their 96-dpi proportions (labels 20, picture 150, title 45, bar 5 px at 96 dpi; rounding may differ by 1 px)
                var heights = Priv.Field<Panel>(upd, "panelMain").Controls.Cast<Control>().OrderBy(c => c.Top).Select(c => c.Height).ToArray();
                var designed = new[] { 20, 150, 45, 20, 5 };
                Assert.Equal(designed.Length, heights.Length);
                for (var i = 0; i < designed.Length; i++)
                {
                    Assert.InRange(heights[i], designed[i] * 3 / 2 - 1, designed[i] * 3 / 2 + 1);
                }
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
            // 96 dpi: the 1.0 pixels
            using var at96 = new YANUpdateScreen();
            if (at96.CurrentAutoScaleDimensions.Width == 96)
            {
                at96.ShowValues(50, null, YANConstant.W_UPDATE_SCR / 2);
                Assert.Equal(YANConstant.W_UPDATE_SCR / 2, at96.pnlProgressBar.Width);
            }
        });
    }

}
