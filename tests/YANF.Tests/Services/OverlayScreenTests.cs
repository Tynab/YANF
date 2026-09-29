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
    }
}
