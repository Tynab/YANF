using Xunit;
using YANF.Script.Service;
using static YANF.Tests.Services.OverlayKit;

namespace YANF.Tests.Services
{
    public class YANWaitScrServiceTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void OnOff_Twice_AndOnLoaderTwice(int corner) => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANWaitScrService { Corner = corner };
            // before any OnLoader
            svc.OffLoader();
            svc.OnLoader(p);
            var h1 = HostOf(svc);
            svc.OffLoader();
            svc.OffLoader();
            AssertAllClosed("on + off", h1);
            svc.OnLoader(p);
            var h2 = HostOf(svc);
            svc.OnLoader(p);
            var h3 = HostOf(svc);
            Assert.True(Poll.Until(() => Finished(h2)), "the first overlay was not closed by the second OnLoader");
            svc.OffLoader();
            AssertAllClosed("twice", h2, h3);
        });
    }
}
