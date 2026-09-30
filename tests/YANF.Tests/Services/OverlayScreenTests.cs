using System;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using static YANF.Tests.Services.OverlayKit;

namespace YANF.Tests.Services
{
    public class OverlayScreenTests
    {
        // The public constructors still work, the round region is set from the corner, and a second close is a no-op
        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        public void PublicConstructors_AndCloseAfterDispose(int corner) => Sta.Run(() =>
        {
            using var p = NewParent();
            var load = new YANLoadScreen(p, corner, false);
            Assert.Equal(p.Bounds, load.Bounds);
            Assert.Equal(corner > 0, load.Region != null);
            Assert.False(load.TopMost);
            load.Dispose();
            // already disposed: no-op
            load.Frm_Close();
            var wait = new YANWaitScreen(p, corner, true);
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
            using (var load = new YANLoadScreen(p, 0, false))
            {
                Assert.IsAssignableFrom<YANOverlayScreen>(load);
            }
            using (var wait = new YANWaitScreen(p, 0, false))
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
            using var load = new YANLoadScreen(p, 0, false);
            load.SetProgress(42, "not shown");
            Assert.Equal("42%", load.lblPercent.Text);
            using var wait = new YANWaitScreen(p, 0, false);
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
    }

}
