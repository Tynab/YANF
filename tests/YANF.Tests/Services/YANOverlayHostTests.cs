using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Xunit;
using YANF.Screen;
using YANF.Script.Service;
using static YANF.Tests.Services.OverlayKit;

namespace YANF.Tests.Services
{
    // The internal host shared by the Load, Wait and Update services (InternalsVisibleTo)
    public class YANOverlayHostTests
    {
        // PublishValue before the handle exists: the latest value is kept and applied when the screen loads
        [Fact]
        public void PublishBeforeStart_AppliedOnLoad() => Guarded(() =>
        {
            var bounds = new Rectangle(50, 50, 300, 200);
            var h = new YANOverlayHost<YANLoadScreen>(() => new YANLoadScreen(bounds, 0, false));
            h.Publish(s => s.lblPercent.Text = "1%");
            h.Publish(s => s.lblPercent.Text = "42%");
            h.Start();
            var scr = (YANLoadScreen)ScreenOf(h);
            Assert.NotNull(scr);
            Assert.True(Poll.Until(() => OnOverlay(scr, () => scr.lblPercent.Text) == "42%"), "queued update not applied: " + OnOverlay(scr, () => scr.lblPercent.Text));
            h.Close();
            h.Close();
            AssertAllClosed("publish before start", h);
        });

        // Close before Start: no thread, no window
        [Fact]
        public void CloseBeforeStart_ShowsNothing() => Guarded(() =>
        {
            var created = 0;
            var h = new YANOverlayHost<YANWaitScreen>(() =>
            {
                Interlocked.Increment(ref created);
                return new YANWaitScreen(new Rectangle(0, 0, 200, 100), 0, false);
            });
            h.Close();
            h.Start();
            Assert.True(Finished(h), "host not finished");
            Assert.Equal(0, created);
        });

        // A failure while building the screen surfaces from Start on the caller's thread and leaves nothing behind
        [Fact]
        public void CreationFailure_ThrowsFromStart() => Guarded(() =>
        {
            var h = new YANOverlayHost<YANWaitScreen>(() => throw new InvalidOperationException("screen constructor failed"));
            var ex = Assert.Throws<InvalidOperationException>(() => h.Start());
            Assert.Equal("screen constructor failed", ex.Message);
            Assert.True(Finished(h), "host not finished after a failed start");
            h.Publish(s => throw new Exception("an update after a failed start must be dropped"));
            h.Close();
            h.Close();
            AssertAllClosed("creation failure", h);
        });

        // The SystemEvents pre-initialisation (skipped where UserInteractive is false) never throws or hangs
        [Fact]
        public void SystemEventsInit_IsHarmless()
        {
            var type = typeof(YANOverlayHost<YANWaitScreen>);
            Exception error = null;
            var t = new Thread(() =>
            {
                try
                {
                    Priv.CallStatic(type, "EnsureSystemEvents");
                    Priv.CallStatic(type, "EnsureSystemEvents");
                    Priv.CallStatic(type, "TouchSystemEvents");
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            t.Start();
            Assert.True(t.Join(10000), "SystemEvents initialisation hung");
            Assert.Null(error);
        }

        // A load slower than the ready timeout: Start returns at the timeout without throwing, and the late screen closes itself
        [Fact]
        public void SlowLoad_TimesOut_ThenClosesItself() => Guarded(() =>
        {
            var timeout = Priv.StaticField<int>(typeof(YANOverlayHost<YANWaitScreen>), "READY_TIMEOUT");
            var h = new YANOverlayHost<YANWaitScreen>(() =>
            {
                Thread.Sleep(timeout + 3000);
                return new YANWaitScreen(new Rectangle(0, 0, 200, 100), 0, true);
            });
            var sw = Stopwatch.StartNew();
            h.Start();
            sw.Stop();
            Assert.InRange(sw.ElapsedMilliseconds, timeout - 500, timeout + 2500);
            h.Publish(s => throw new Exception("an update after the timeout must be dropped"));
            AssertAllClosed("slow load", h);
        });
    }
}
