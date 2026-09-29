using System;
using System.Drawing;
using Xunit;
using YANF.Screen;
using YANF.Script;
using YANF.Script.Service;
using static YANF.Tests.Services.OverlayKit;

namespace YANF.Tests.Services
{
    // 1.0.2 never passed the parent to the update screen and wrote the three values from the calling thread
    public class YANUpdScrServiceTests
    {
        [Fact]
        public void Publish_SetsAllThreeValues_AndCentresOnTheParent() => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANUpdScrService();
            // before OnLoader: no-op
            svc.PublishValue(1, "before", 1);
            svc.OnLoader(p);
            var h = HostOf(svc);
            var scr = (YANUpdateScreen)ScreenOf(h);
            var bad = Watch(scr);
            svc.PublishValue(50, "68.5 MB / 137 MB", YANConstant.W_UPDATE_SCR / 2);
            Assert.True(Poll.Until(() => OnOverlay(scr, () => scr.lblPercent.Text) == "50%"), "percent not applied");
            Assert.Equal(YANConstant.W_UPDATE_SCR / 2, OnOverlay(scr, () => scr.pnlProgressBar.Width));
            Assert.Equal("68.5 MB / 137 MB", OnOverlay(scr, () => scr.lblCapacity.Text));
            // centred on the parent, kept inside the working area of its screen
            var b = OnOverlay(scr, () => scr.Bounds);
            var area = System.Windows.Forms.Screen.FromRectangle(p.Bounds).WorkingArea;
            var expected = new Point(
                Math.Max(area.Left, Math.Min((p.Left + p.Right - b.Width) / 2, area.Right - b.Width)),
                Math.Max(area.Top, Math.Min((p.Top + p.Bottom - b.Height) / 2, area.Bottom - b.Height)));
            Assert.Equal(expected, b.Location);
            svc.OffLoader();
            svc.PublishValue(100, "after", 360);
            svc.OffLoader();
            AssertAllClosed("update", h);
            AssertAffinity(bad);
        });

        [Fact]
        public void NullParent_StillWorks() => Guarded(() =>
        {
            var svc = new YANUpdScrService();
            svc.OnLoader(null);
            var h = HostOf(svc);
            svc.PublishValue(10, "x", 36);
            svc.OnLoader(null);
            var h2 = HostOf(svc);
            svc.OffLoader();
            AssertAllClosed("null parent", h, h2);
        });
    }
}
