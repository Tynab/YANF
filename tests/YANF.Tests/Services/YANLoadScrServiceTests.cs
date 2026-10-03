using System;
using System.Linq;
using System.Threading;
using Xunit;
using YANF.Screen;
using YANF.Script.Service;
using static YANF.Tests.Services.OverlayKit;

namespace YANF.Tests.Services
{
    // 1.0.2 started the overlay on a foreground MTA thread and returned at once: OffLoader right after OnLoader did nothing,
    // PublishValue threw or wrote across threads, and the screen could be owned by a window of another thread
    public class YANLoadScrServiceTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(20)]
        public void OnThenImmediateOff(int corner) => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANLoadScrService { Corner = corner };
            svc.OnLoader(p);
            var h = HostOf(svc);
            Assert.NotNull(h);
            svc.OffLoader();
            Assert.Null(HostOf(svc));
            AssertAllClosed("on + off", h);
        });

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        public void Shows_AtParentBounds_WithoutOwner(int corner) => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANLoadScrService { Corner = corner, IsTop = true };
            svc.OnLoader(p);
            var h = HostOf(svc);
            var scr = ScreenOf(h);
            Assert.True(scr != null && !scr.IsDisposed, "screen not loaded when OnLoader returned");
            Assert.Equal(p.Bounds, OnOverlay(scr, () => scr.Bounds));
            Assert.True(OnOverlay(scr, () => scr.TopMost), "IsTop not applied");
            Assert.True(OnOverlay(scr, () => scr.Owner == null), "the overlay got an owner");
            Assert.Equal(corner > 0, OnOverlay(scr, () => scr.Region != null));
            var mailbox = MailboxOf(h);
            Assert.NotNull(mailbox);
            Assert.False(mailbox.Visible, "the mailbox window must stay hidden");
            svc.OffLoader();
            AssertAllClosed("bounds", h);
        });

        [Fact]
        public void PublishOrOff_BeforeOnLoader_IsNoOp() => Guarded(() =>
        {
            var svc = new YANLoadScrService();
            svc.PublishValue(5, null, 0);
            svc.OffLoader();
            svc.OffLoader();
        });

        [Fact]
        public void Publish_LatestValueWins() => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANLoadScrService();
            svc.OnLoader(p);
            var h = HostOf(svc);
            var scr = (YANLoadScreen)ScreenOf(h);
            var bad = Watch(scr);
            for (var i = 0; i <= 100; i++)
            {
                svc.PublishValue(i, null, 0);
            }
            Assert.True(Poll.Until(() => OnOverlay(scr, () => scr.lblPercent.Text) == "100%"), "label did not reach 100%: " + OnOverlay(scr, () => scr.lblPercent.Text));
            svc.OffLoader();
            // after the close: dropped
            svc.PublishValue(7, null, 0);
            AssertAllClosed("publish", h);
            AssertAffinity(bad);
        });

        [Fact]
        public void Publish_FromWorkerThreads_WhileClosing() => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANLoadScrService();
            svc.OnLoader(p);
            var h = HostOf(svc);
            var bad = Watch(ScreenOf(h));
            Exception error = null;
            var workers = Enumerable.Range(0, 4).Select(_ => new Thread(() =>
            {
                try
                {
                    for (var i = 0; i < 2000; i++)
                    {
                        svc.PublishValue(i % 101, null, 0);
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            })).ToList();
            workers.ForEach(w => w.Start());
            Thread.Sleep(30);
            // close while the workers keep publishing
            svc.OffLoader();
            workers.ForEach(w => w.Join());
            Assert.Null(error);
            AssertAllClosed("workers", h);
            AssertAffinity(bad);
        });

        // A worker publishes 1..100 then closes: the last value is on screen before the overlay goes (the close must not drop it)
        [Fact]
        public void PublishThenOff_FromWorker_ShowsLastValue() => Guarded(() =>
        {
            using var p = NewParent();
            for (var n = 0; n < 6; n++)
            {
                var svc = new YANLoadScrService();
                svc.OnLoader(p);
                var h = HostOf(svc);
                var scr = (YANLoadScreen)ScreenOf(h);
                var bad = Watch(scr);
                string last = null, atDispose = null;
                scr.Invoke(new Action(() =>
                {
                    scr.lblPercent.TextChanged += (s, e) => last = scr.lblPercent.Text;
                    scr.Disposed += (s, e) => Volatile.Write(ref atDispose, last ?? "(no value shown)");
                }));
                // odd runs: let the fade-in finish so the overlay is idle when the values arrive
                if (n % 2 == 1)
                {
                    Thread.Sleep(400);
                }
                var worker = new Thread(() =>
                {
                    for (var i = 1; i <= 100; i++)
                    {
                        svc.PublishValue(i, null, 0);
                    }
                    svc.OffLoader();
                });
                worker.Start();
                worker.Join();
                AssertAllClosed("publish then off #" + n, h);
                Assert.Equal("100%", Volatile.Read(ref atDispose));
                AssertAffinity(bad);
            }
        });

        [Fact]
        public void OffLoader_Twice() => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANLoadScrService();
            svc.OnLoader(p);
            var h = HostOf(svc);
            svc.OffLoader();
            svc.OffLoader();
            AssertAllClosed("double off", h);
            svc.OffLoader();
        });

        [Fact]
        public void OnLoader_Twice_ClosesThePreviousOverlay() => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANLoadScrService();
            svc.OnLoader(p);
            var h1 = HostOf(svc);
            var bad1 = Watch(ScreenOf(h1));
            svc.OnLoader(p);
            var h2 = HostOf(svc);
            var bad2 = Watch(ScreenOf(h2));
            Assert.NotSame(h1, h2);
            Assert.True(Poll.Until(() => Finished(h1)), "the first overlay was not closed by the second OnLoader");
            Assert.False(Finished(h2), "the second overlay closed too early");
            Assert.True(Poll.Until(() => OpenOverlays().Length == 1), "expected exactly one open overlay, got " + OpenOverlays().Length);
            svc.PublishValue(3, null, 0);
            svc.OffLoader();
            AssertAllClosed("twice", h1, h2);
            AssertAffinity(bad1);
            AssertAffinity(bad2);
        });

        [Fact]
        public void Close_DuringFadeIn() => Guarded(() =>
        {
            using var p = NewParent();
            var svc = new YANLoadScrService();
            foreach (var delay in new[] { 0, 1, 5, 20, 60, 150 })
            {
                svc.OnLoader(p);
                var h = HostOf(svc);
                if (delay > 0)
                {
                    Thread.Sleep(delay);
                }
                svc.OffLoader();
                AssertAllClosed("close after " + delay + " ms", h);
            }
        });

        [Fact]
        public void OffLoader_FromOtherThread_DuringOnLoader() => Guarded(() =>
        {
            using var p = NewParent();
            for (var n = 0; n < 5; n++)
            {
                var svc = new YANLoadScrService();
                using var go = new ManualResetEventSlim();
                var closer = new Thread(() =>
                {
                    go.Wait();
                    for (var i = 0; i < 200; i++)
                    {
                        svc.OffLoader();
                        svc.PublishValue(i, null, 0);
                        Thread.SpinWait(200);
                    }
                });
                closer.Start();
                go.Set();
                svc.OnLoader(p);
                var h = HostOf(svc);
                closer.Join();
                svc.OffLoader();
                AssertAllClosed("concurrent off #" + n, h);
            }
        });

        [Fact]
        public void NullParent_Throws() => Guarded(() =>
        {
            var svc = new YANLoadScrService();
            Assert.Throws<ArgumentNullException>(() => svc.OnLoader(null));
            svc.OffLoader();
        });
    }
}
