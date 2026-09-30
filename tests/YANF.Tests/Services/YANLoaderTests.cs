using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using YANF.Script;
using static System.Windows.Forms.FormStartPosition;
using static System.Windows.Forms.FormWindowState;
using static YANF.Script.YANLoaderKind;
using static YANF.Tests.Services.LoaderKit;

namespace YANF.Tests.Services
{
    using Control = System.Windows.Forms.Control;

    // YANLoader (1.1): the owned overlay on the owner's own UI thread
    public class YANLoaderTests
    {
        [Fact]
        public void Options_Defaults()
        {
            var o = new YANLoaderOptions();
            Assert.Equal(Load, o.Kind);
            Assert.Equal(250, o.ShowDelay);
            Assert.Equal(0, o.Corner);
            Assert.Equal(200, o.FadeDuration);
        }

        // Work that ends within the delay shows nothing; the owner takes no input meanwhile and gets it back after
        [Fact]
        public void FastWork_ShowsNothing() => Run(() =>
        {
            using var owner = ShowOwner();
            foreach (var options in new[] { null, new YANLoaderOptions { ShowDelay = 300 } })
            {
                YANLoaderScope scope = null;
                bool isBlocked = false, isShown = false;
                var task = owner.RunWithLoaderAsync((p, ct) =>
                {
                    scope = Assert.IsType<YANLoaderScope>(p);
                    scope.Screen.VisibleChanged += (s, e) => isShown |= scope.Screen.Visible;
                    isBlocked = !IsEnabled(owner) && scope.IsOwnerBlocked;
                    return Task.Delay(10, ct);
                }, options, CancellationToken.None);
                Complete(task);
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                Assert.True(isBlocked, "the owner took input while the work ran");
                Assert.True(scope.Screen.IsDisposed, "the unused screen was not disposed");
                Assert.True(IsEnabled(owner), "the owner was not given back");
                // the stopped delay timer never shows it later
                PumpFor(400);
                Assert.False(isShown, "fast work showed the screen");
            }
        });

        // Slow work: the screen appears after the delay as an owned window over (Load, Wait) or centred on (Update) the owner
        [Theory]
        [InlineData(Load)]
        [InlineData(Wait)]
        [InlineData(Update)]
        public void SlowWork_ShowsTheScreen_AfterTheDelay(YANLoaderKind kind) => Run(() =>
        {
            using var owner = ShowOwner();
            var gate = new TaskCompletionSource<int>();
            YANLoaderScope scope = null;
            var sw = Stopwatch.StartNew();
            var task = owner.RunWithLoaderAsync((p, ct) =>
            {
                scope = (YANLoaderScope)p;
                return gate.Task;
            }, new YANLoaderOptions { Kind = kind, ShowDelay = 150 }, CancellationToken.None);
            var scr = scope.Screen;
            Assert.IsType(kind switch { Wait => typeof(YANWaitScreen), Update => typeof(YANUpdateScreen), _ => typeof(YANLoadScreen) }, scr);
            Assert.False(scr.Visible, "shown before the delay");
            Assert.True(PumpUntil(() => scr.Visible), "never shown");
            Assert.True(sw.ElapsedMilliseconds >= 140, $"shown after {sw.ElapsedMilliseconds} ms, before the 150 ms delay");
            Assert.Same(owner, scr.Owner);
            Assert.False(scr.TopMost, "an owned screen must not be top-most over other applications");
            Assert.False(IsEnabled(owner), "the owner takes input under the screen");
            Assert.Equal(Expected(kind, owner, scr), scr.Bounds);
            gate.SetResult(42);
            Complete(task);
            Assert.Equal(42, task.Result);
            Assert.True(scr.IsDisposed, "the screen was still there when the task ended");
            Assert.True(IsEnabled(owner), "the owner was not given back");
        });

        // The screen follows the owner when it is moved, resized, hidden and shown again
        [Theory]
        [InlineData(Load)]
        [InlineData(Update)]
        public void Screen_FollowsTheOwner(YANLoaderKind kind) => Run(() =>
        {
            using var owner = ShowOwner();
            var scope = YANLoader.Show(owner, new YANLoaderOptions { Kind = kind, ShowDelay = 0, FadeDuration = 0 });
            var scr = scope.Screen;
            Assert.True(PumpUntil(() => scr.Visible), "not shown with ShowDelay 0");
            owner.Location = new Point(owner.Left + 37, owner.Top + 21);
            Assert.True(PumpUntil(() => scr.Bounds == Expected(kind, owner, scr)), "did not follow the move: " + scr.Bounds);
            owner.Size = new Size(520, 410);
            Assert.True(PumpUntil(() => scr.Bounds == Expected(kind, owner, scr)), "did not follow the resize: " + scr.Bounds);
            owner.Hide();
            Assert.True(PumpUntil(() => !scr.Visible), "still visible over a hidden owner");
            owner.Show();
            Assert.True(PumpUntil(() => scr.Visible), "not shown again with the owner");
            Assert.Equal(Expected(kind, owner, scr), scr.Bounds);
            scope.Dispose();
            Assert.True(scr.IsDisposed);
            // no longer follows
            owner.Location = new Point(owner.Left + 5, owner.Top + 5);
            PumpFor(50);
        });

        // Hidden while the owner is minimized, back when it is restored
        [Fact]
        public void Screen_HidesWhileTheOwnerIsMinimized() => Run(() =>
        {
            using var owner = ShowOwner();
            using var scope = YANLoader.Show(owner, new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0 });
            var scr = scope.Screen;
            Assert.True(PumpUntil(() => scr.Visible));
            owner.WindowState = Minimized;
            Assert.True(PumpUntil(() => owner.WindowState == Minimized, 3000), "the owner did not minimize");
            Assert.True(PumpUntil(() => !scr.Visible), "still visible over a minimized owner");
            owner.WindowState = Normal;
            Assert.True(PumpUntil(() => scr.Visible), "not shown again when the owner was restored");
            Assert.Equal(Expected(Load, owner, scr), scr.Bounds);
        });

        // The corner region is rebuilt for every size (the screen covers the owner's visible frame: on Windows 10 and later a sizable
        // owner is wider and taller than that by its invisible resize borders)
        [Fact]
        public void Corner_IsKeptWhenTheScreenIsResized() => Run(() =>
        {
            using var owner = ShowOwner();
            using var scope = YANLoader.Show(owner, new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0, Corner = 24 });
            var scr = scope.Screen;
            Assert.True(PumpUntil(() => scr.Visible));
            AssertRegionFits(scr);
            var before = scr.Size;
            owner.Size = new Size(430, 330);
            Assert.True(PumpUntil(() => scr.Bounds == Expected(Load, owner, scr)), "did not follow the resize: " + scr.Bounds);
            Assert.NotEqual(before, scr.Size);
            AssertRegionFits(scr);
        });

        // Report and SetProgress from a worker thread are shown on the UI thread; the latest value wins
        [Fact]
        public void Progress_FromAWorkerThread_IsShownOnTheUiThread() => Run(() =>
        {
            using var owner = ShowOwner();
            var scope = YANLoader.Show(owner, new YANLoaderOptions { Kind = Update, ShowDelay = 0, FadeDuration = 0 });
            var scr = (YANUpdateScreen)scope.Screen;
            var uiThread = Thread.CurrentThread.ManagedThreadId;
            var bad = new List<string>();
            void Check(string what)
            {
                if (Thread.CurrentThread.ManagedThreadId != uiThread)
                {
                    lock (bad)
                    {
                        bad.Add(what + " on thread " + Thread.CurrentThread.ManagedThreadId);
                    }
                }
            }
            scr.lblPercent.TextChanged += (s, e) => Check("percent");
            scr.lblCapacity.TextChanged += (s, e) => Check("detail");
            scr.pnlProgressBar.SizeChanged += (s, e) => Check("bar");
            OnWorker(() =>
            {
                IProgress<int> progress = scope;
                for (var i = 0; i <= 100; i++)
                {
                    progress.Report(i);
                    if (i == 50)
                    {
                        scope.SetProgress(50, "68.5 MB / 137 MB");
                    }
                }
            });
            Assert.True(PumpUntil(() => scr.lblPercent.Text == "100%"), "the last value was not shown: " + scr.lblPercent.Text);
            // Report keeps the detail; the bar is sized from the screen's own width
            Assert.Equal("68.5 MB / 137 MB", scr.lblCapacity.Text);
            Assert.Equal(scr.ClientSize.Width, scr.pnlProgressBar.Width);
            lock (bad)
            {
                Assert.True(bad.Count == 0, "cross-thread access: " + string.Join(" | ", bad.Take(5)));
            }
            // on the UI thread: shown at once and clamped
            scope.Report(40);
            Assert.Equal("40%", scr.lblPercent.Text);
            Assert.Equal((int)Math.Ceiling(scr.ClientSize.Width * 0.4), scr.pnlProgressBar.Width);
            scope.Report(150);
            Assert.Equal("100%", scr.lblPercent.Text);
            scope.SetProgress(-5, null);
            Assert.Equal("0%", scr.lblPercent.Text);
            Assert.Equal("", scr.lblCapacity.Text);
            Assert.Equal(0, scr.pnlProgressBar.Width);
            scope.Dispose();
            Assert.True(scr.IsDisposed);
            // after the close: dropped, from any thread
            scope.Report(10);
            scope.SetProgress(20, "late");
            OnWorker(() => scope.Report(30));
            PumpFor(50);
        });

        // An exception of the work (thrown by its task or by the delegate itself) reaches the caller; the owner is given back
        [Fact]
        public void WorkFailure_ReachesTheCaller() => Run(() =>
        {
            using var owner = ShowOwner();
            foreach (var fade in new[] { 0, 60 })
            {
                var options = new YANLoaderOptions { ShowDelay = 0, FadeDuration = fade };
                YANLoaderScope scope = null;
                var t1 = owner.RunWithLoaderAsync((p, ct) =>
                {
                    scope = (YANLoaderScope)p;
                    return Task.Run(() =>
                    {
                        Thread.Sleep(80);
                        throw new ApplicationException("pool");
                    });
                }, options, CancellationToken.None);
                Complete(t1);
                Assert.True(t1.IsFaulted);
                Assert.Equal("pool", Assert.IsType<ApplicationException>(t1.Exception.InnerException).Message);
                Assert.True(scope.Screen.IsDisposed, "the screen outlived the failed work");
                Assert.True(IsEnabled(owner), "the owner was not given back after a failure");
            }
            var sync = new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0 };
            var t2 = owner.RunWithLoaderAsync<int>((p, ct) => throw new ArgumentException("sync"), sync, CancellationToken.None);
            Complete(t2);
            Assert.IsType<ArgumentException>(t2.Exception.InnerException);
            Assert.True(IsEnabled(owner));
            var t3 = owner.RunWithLoaderAsync((p, ct) => null, sync, CancellationToken.None);
            Complete(t3);
            Assert.IsType<InvalidOperationException>(t3.Exception.InnerException);
            Assert.True(IsEnabled(owner));
        });

        // Cancellation of the work reaches the caller as a cancelled task; an already cancelled token shows nothing
        [Fact]
        public void Cancellation_ReachesTheCaller() => Run(() =>
        {
            using var owner = ShowOwner();
            using var cts = new CancellationTokenSource();
            YANLoaderScope scope = null;
            var task = owner.RunWithLoaderAsync((p, ct) =>
            {
                scope = (YANLoaderScope)p;
                return Task.Delay(Timeout.Infinite, ct);
            }, new YANLoaderOptions { ShowDelay = 0 }, cts.Token);
            Assert.True(PumpUntil(() => scope.Screen.Visible));
            cts.Cancel();
            Complete(task);
            Assert.True(task.IsCanceled, "status " + task.Status);
            Assert.True(scope.Screen.IsDisposed);
            Assert.True(IsEnabled(owner));
            var calls = 0;
            var t2 = owner.RunWithLoaderAsync<int>((p, ct) =>
            {
                calls++;
                return Task.FromResult(1);
            }, null, cts.Token);
            Assert.True(t2.IsCanceled);
            Assert.Equal(0, calls);
            Assert.True(IsEnabled(owner));
            Assert.Empty(OpenScreens());
        });

        // The owner closes while the work runs: the screen goes with it, the work still completes for the caller
        [Fact]
        public void OwnerClosed_DuringTheWork() => Run(() =>
        {
            var owner = ShowOwner();
            try
            {
                var gate = new TaskCompletionSource<int>();
                YANLoaderScope scope = null;
                var task = owner.RunWithLoaderAsync((p, ct) =>
                {
                    scope = (YANLoaderScope)p;
                    return gate.Task;
                }, new YANLoaderOptions { ShowDelay = 0 }, CancellationToken.None);
                Assert.True(PumpUntil(() => scope.Screen.Visible));
                owner.Close();
                Assert.True(PumpUntil(() => scope.Screen.IsDisposed), "the screen outlived its owner");
                Assert.False(scope.IsOwnerBlocked);
                gate.SetResult(7);
                Complete(task);
                Assert.Equal(7, task.Result);
            }
            finally
            {
                owner.Dispose();
            }
            // closed before the delay elapsed: nothing is shown, the work's failure still reaches the caller
            var owner2 = ShowOwner();
            try
            {
                var gate2 = new TaskCompletionSource<bool>();
                YANLoaderScope scope2 = null;
                var task2 = owner2.RunWithLoaderAsync((p, ct) =>
                {
                    scope2 = (YANLoaderScope)p;
                    return gate2.Task;
                }, new YANLoaderOptions { ShowDelay = 100 }, CancellationToken.None);
                var isShown = false;
                scope2.Screen.VisibleChanged += (s, e) => isShown = true;
                owner2.Close();
                PumpFor(300);
                Assert.False(isShown, "shown over a closed owner");
                Assert.True(scope2.Screen.IsDisposed);
                gate2.SetException(new ApplicationException("late"));
                Complete(task2);
                Assert.IsType<ApplicationException>(task2.Exception.InnerException);
            }
            finally
            {
                owner2.Dispose();
            }
        });

        // Dispose gives the owner back at once; later calls do nothing; a scope disposed within the delay never shows
        [Fact]
        public void Dispose_Twice() => Run(() =>
        {
            using var owner = ShowOwner();
            var scope = YANLoader.Show(owner, new YANLoaderOptions { ShowDelay = 0 });
            var scr = scope.Screen;
            Assert.True(PumpUntil(() => scr.Visible));
            Assert.False(IsEnabled(owner));
            scope.Dispose();
            Assert.True(IsEnabled(owner), "Dispose did not give the owner back at once");
            scope.Dispose();
            Assert.True(PumpUntil(() => scr.IsDisposed), "the screen was not closed");
            scope.Dispose();
            var scope2 = YANLoader.Show(owner);
            var isShown = false;
            scope2.Screen.VisibleChanged += (s, e) => isShown = true;
            Assert.False(IsEnabled(owner));
            scope2.Dispose();
            scope2.Dispose();
            Assert.True(scope2.Screen.IsDisposed);
            Assert.True(IsEnabled(owner));
            PumpFor(400);
            Assert.False(isShown, "a scope disposed within the delay showed its screen");
        });

        // Used from another thread: Show and RunWithLoaderAsync throw at once; Dispose throws but still closes on the UI thread
        [Fact]
        public void OtherThread_Throws() => Run(() =>
        {
            using var owner = ShowOwner();
            Exception show = null, run = null, report = null, dispose = null;
            OnWorker(() =>
            {
                show = Record(() => YANLoader.Show(owner));
                run = Record(() => owner.RunWithLoaderAsync((p, ct) => Task.FromResult(0)));
            });
            Assert.IsType<InvalidOperationException>(show);
            Assert.IsType<InvalidOperationException>(run);
            Assert.True(IsEnabled(owner));
            Assert.Empty(OpenScreens());
            var scope = YANLoader.Show(owner, new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0 });
            Assert.True(PumpUntil(() => scope.Screen.Visible));
            OnWorker(() =>
            {
                report = Record(() =>
                {
                    scope.Report(5);
                    scope.SetProgress(6, "x");
                });
                dispose = Record(scope.Dispose);
            });
            Assert.Null(report);
            Assert.IsType<InvalidOperationException>(dispose);
            Assert.True(PumpUntil(() => IsEnabled(owner) && scope.Screen.IsDisposed), "a Dispose on the wrong thread left the owner blocked");
            scope.Dispose();
        });

        [Fact]
        public void Arguments() => Run(() =>
        {
            Assert.Throws<ArgumentNullException>("owner", () => YANLoader.Show(null));
            ThrowsNow<ArgumentNullException>("owner", () => _ = ((Form)null).RunWithLoaderAsync((p, ct) => Task.FromResult(0)));
            using var owner = ShowOwner();
            ThrowsNow<ArgumentNullException>("work", () => _ = owner.RunWithLoaderAsync((Func<IProgress<int>, CancellationToken, Task>)null));
            ThrowsNow<ArgumentNullException>("work", () => _ = owner.RunWithLoaderAsync<int>(null));
            Assert.Throws<ArgumentOutOfRangeException>("options", () => YANLoader.Show(owner, new YANLoaderOptions { Kind = (YANLoaderKind)7 }));
            Assert.True(IsEnabled(owner));
            var gone = new Form();
            gone.Dispose();
            Assert.Throws<ObjectDisposedException>(() => YANLoader.Show(gone));
            // negative values count as 0: shown at once, no fade, no region
            using var scope = YANLoader.Show(owner, new YANLoaderOptions { ShowDelay = -1, FadeDuration = -5, Corner = -3 });
            Assert.True(scope.Screen.Visible);
            Assert.Null(scope.Screen.Region);
        });

        // Nested scopes share the owner's block (the owner is given back when the last one closes); an owner disabled by someone else
        // is left as it is
        [Fact]
        public void OwnerBlockedAlready_IsLeftAsItIs() => Run(() =>
        {
            using var owner = ShowOwner();
            var none = new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0 };
            var outer = YANLoader.Show(owner, none);
            var inner = YANLoader.Show(owner, new YANLoaderOptions { Kind = Wait, ShowDelay = 0, FadeDuration = 0 });
            Assert.True(outer.IsOwnerBlocked);
            Assert.True(inner.IsOwnerBlocked);
            inner.Dispose();
            Assert.False(IsEnabled(owner), "the inner scope gave the owner back while the outer one is open");
            outer.Dispose();
            Assert.True(IsEnabled(owner));
            _ = YANLoaderScope.Native.EnableWindow(owner.Handle, false);
            try
            {
                var scope = YANLoader.Show(owner, none);
                scope.Dispose();
                Assert.False(IsEnabled(owner), "enabled a window that another party had disabled");
            }
            finally
            {
                _ = YANLoaderScope.Native.EnableWindow(owner.Handle, true);
            }
        });

        // The screen fades in, and the task ends only once the screen has faded out and is gone
        [Fact]
        public void Screen_FadesIn_ThenOut() => Run(() =>
        {
            using var owner = ShowOwner();
            var gate = new TaskCompletionSource<bool>();
            YANLoaderScope scope = null;
            var task = owner.RunWithLoaderAsync((p, ct) =>
            {
                scope = (YANLoaderScope)p;
                return gate.Task;
            }, new YANLoaderOptions { ShowDelay = 0, FadeDuration = 100 }, CancellationToken.None);
            Assert.True(PumpUntil(() => scope.Screen.Visible && scope.Screen.Opacity >= 1), "not faded in: " + scope.Screen.Opacity);
            gate.SetResult(true);
            Complete(task);
            Assert.True(scope.Screen.IsDisposed);
            Assert.True(IsEnabled(owner));
        });

        // RunWithLoaderAsync keeps the owner blocked while the screen fades out, so nothing reaches the owner (around the Update box)
        // before the awaiting code runs; Dispose gives it back at once, also during that fade
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Owner_StaysBlocked_UntilTheScreenIsGone(bool isDisposedDuringTheFade) => Run(() =>
        {
            var oldEffects = YANDisplay.UIEffectsOverride;
            YANDisplay.UIEffectsOverride = true;
            try
            {
                using var owner = ShowOwner();
                var gate = new TaskCompletionSource<bool>();
                YANLoaderScope scope = null;
                var task = owner.RunWithLoaderAsync((p, ct) =>
                {
                    scope = (YANLoaderScope)p;
                    return gate.Task;
                }, new YANLoaderOptions { Kind = Update, ShowDelay = 0, FadeDuration = 150 }, CancellationToken.None);
                var scr = scope.Screen;
                Assert.True(PumpUntil(() => scr.Visible && scr.Opacity >= 1), "not faded in: " + scr.Opacity);
                gate.SetResult(true);
                if (isDisposedDuringTheFade)
                {
                    Assert.True(PumpUntil(() => scr.IsClosing), "the close did not start");
                    Assert.False(IsEnabled(owner));
                    scope.Dispose();
                    Assert.True(IsEnabled(owner), "Dispose did not give the owner back at once");
                    Assert.False(scr.IsDisposed, "the fade-out was cut short");
                    Complete(task);
                }
                else
                {
                    bool isFading = false, isGivenBackEarly = false;
                    Assert.True(PumpUntil(() =>
                    {
                        isFading |= !scr.IsDisposed && scr.IsClosing && scr.Opacity < 1;
                        isGivenBackEarly |= !scr.IsDisposed && IsEnabled(owner);
                        return task.IsCompleted;
                    }), "the loader task did not end");
                    Assert.True(isFading, "the screen did not fade out");
                    Assert.False(isGivenBackEarly, "the owner took input while the screen was fading out");
                }
                Assert.Equal(TaskStatus.RanToCompletion, task.Status);
                Assert.True(scr.IsDisposed);
                Assert.True(IsEnabled(owner), "the owner was not given back");
            }
            finally
            {
                YANDisplay.UIEffectsOverride = oldEffects;
            }
        });

        // A dialog that the work opens before the screen has appeared is never covered: the screen waits until the dialog is closed,
        // then one more delay (the work often ends right after the dialog)
        [Fact]
        public void DialogOfTheWork_IsNotCovered() => Run(() =>
        {
            const int DELAY = 100;
            using var owner = ShowOwner();
            var gate = new TaskCompletionSource<bool>();
            YANLoaderScope scope = null;
            Form dlg = null;
            bool isDialogShown = false, isCovered = false;
            var sinceClose = new Stopwatch();
            var task = owner.RunWithLoaderAsync(async (p, ct) =>
            {
                scope = (YANLoaderScope)p;
                scope.Screen.VisibleChanged += (s, e) => isCovered |= scope.Screen.Visible && dlg != null && !dlg.IsDisposed && dlg.Visible;
                await Task.Delay(20, ct);
                using (dlg = new Form { ShowInTaskbar = false, StartPosition = Manual, Bounds = new Rectangle(owner.Left + 150, owner.Top + 120, 300, 200) })
                using (var closer = new System.Windows.Forms.Timer { Interval = DELAY * 4 })
                {
                    closer.Tick += (s, e) =>
                    {
                        closer.Stop();
                        dlg.Close();
                    };
                    dlg.Shown += (s, e) =>
                    {
                        isDialogShown = true;
                        closer.Start();
                    };
                    _ = dlg.ShowDialog(owner);
                }
                sinceClose.Start();
                await gate.Task;
            }, new YANLoaderOptions { ShowDelay = DELAY, FadeDuration = 0 }, CancellationToken.None);
            Assert.True(PumpUntil(() => scope.Screen.Visible), "not shown after the dialog closed");
            var after = sinceClose.ElapsedMilliseconds;
            Assert.True(isDialogShown, "the dialog was not shown");
            Assert.False(isCovered, "the screen was shown over the work's dialog");
            Assert.True(sinceClose.IsRunning, "shown before the dialog closed");
            Assert.True(after >= DELAY / 2, $"shown {after} ms after the dialog closed, without a new delay");
            Assert.False(IsEnabled(owner));
            gate.SetResult(true);
            Complete(task);
            Assert.True(IsEnabled(owner));
        });

        // Windows that are not forms (a native message box, a common dialog) count when they are enabled and owned by the owner
        // (directly or through another window) or active. Windows open before the scope, tool windows and disabled windows never count
        [Fact]
        public void NativeDialogOfTheWork_IsNotCovered() => Run(() =>
        {
            using var owner = ShowOwner();
            var input = new ExtraWindowsInput(YANLoaderScope.Native);
            YANLoaderScope.Native = input;
            var options = new YANLoaderOptions { ShowDelay = 60, FadeDuration = 0 };
            var toolbox = input.Open(owner.Handle);
            var scope = YANLoader.Show(owner, options);
            _ = input.Open(owner.Handle, isToolWindow: true);
            _ = input.Open(owner.Handle, isEnabled: false);
            var box = input.Open(toolbox);
            PumpFor(300);
            Assert.False(scope.Screen.Visible, "shown over a message box owned by the owner");
            input.Close(box);
            Assert.True(PumpUntil(() => scope.Screen.Visible), "not shown after the message box closed (a window open before, a tool window or a disabled window counted)");
            scope.Dispose();
            // an active window counts whatever its owner; an inactive unowned one does not
            var scope2 = YANLoader.Show(owner, options);
            input.Active = input.Open(IntPtr.Zero);
            PumpFor(300);
            Assert.False(scope2.Screen.Visible, "shown over the active window");
            input.Active = IntPtr.Zero;
            Assert.True(PumpUntil(() => scope2.Screen.Visible), "an inactive unowned window kept the screen hidden");
            scope2.Dispose();
            Assert.True(IsEnabled(owner));
        });

        // Scopes that overlap on one owner (two loads started together, a second click) share its block: the owner is given back when
        // the last one closes, whichever closes first
        [Fact]
        public void OverlappingScopes_TheOwnerIsGivenBackByTheLastOne() => Run(() =>
        {
            using var owner = ShowOwner();
            var none = new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0 };
            var first = YANLoader.Show(owner, none);
            var second = YANLoader.Show(owner, new YANLoaderOptions { Kind = Wait, ShowDelay = 0, FadeDuration = 0 });
            Assert.True(first.IsOwnerBlocked && second.IsOwnerBlocked);
            first.Dispose();
            Assert.False(first.IsOwnerBlocked);
            Assert.False(IsEnabled(owner), "the first scope to close gave the owner back while the second one is open");
            Assert.True(PumpUntil(() => first.Screen.IsDisposed));
            Assert.False(IsEnabled(owner));
            second.Dispose();
            Assert.True(IsEnabled(owner), "the last scope did not give the owner back");
            // RunWithLoaderAsync twice at once (Task.WhenAll): the first work to end does not give the owner back
            var gateA = new TaskCompletionSource<bool>();
            var gateB = new TaskCompletionSource<bool>();
            var options = new YANLoaderOptions { ShowDelay = 50, FadeDuration = 0 };
            var a = owner.RunWithLoaderAsync((p, ct) => gateA.Task, options, CancellationToken.None);
            var b = owner.RunWithLoaderAsync((p, ct) => gateB.Task, options, CancellationToken.None);
            gateA.SetResult(true);
            Complete(a);
            PumpFor(100);
            Assert.False(IsEnabled(owner), "the owner took input while the second work was running");
            gateB.SetResult(true);
            Complete(b);
            Assert.True(IsEnabled(owner));
        });

        // A modal dialog opened while a scope holds the owner (by a timer, from another form) leaves the owner alone, as it is disabled
        // already, and will not enable it when it closes: a scope that closes during the dialog keeps the owner disabled until the
        // dialog is gone, so that the owner takes no input behind it
        [Fact]
        public void ScopeClosedDuringAModalDialog_TheOwnerWaitsForTheDialog() => Run(() =>
        {
            using var owner = ShowOwner();
            var scope = YANLoader.Show(owner, new YANLoaderOptions { ShowDelay = 5000 });
            bool? isEnabledUnderTheDialog = null;
            using var dlg = new Form { ShowInTaskbar = false, StartPosition = Manual, Bounds = new Rectangle(owner.Left + 150, owner.Top + 120, 300, 200) };
            using var timer = new System.Windows.Forms.Timer { Interval = 100 };
            timer.Tick += (s, e) =>
            {
                if (isEnabledUnderTheDialog == null)
                {
                    scope.Dispose();
                    isEnabledUnderTheDialog = IsEnabled(owner);
                }
                else
                {
                    timer.Stop();
                    dlg.Close();
                }
            };
            dlg.Shown += (s, e) => timer.Start();
            _ = dlg.ShowDialog();
            Assert.False(isEnabledUnderTheDialog ?? true, "the scope enabled the owner behind the modal dialog");
            Assert.False(scope.IsOwnerBlocked);
            Assert.True(PumpUntil(() => IsEnabled(owner)), "the owner was not given back when the dialog closed");
            // a dialog open before the scope does not hold the owner back (it is the dialog's own owner here)
            using var dlg2 = new Form { ShowInTaskbar = false, StartPosition = Manual, Bounds = new Rectangle(owner.Left + 150, owner.Top + 120, 300, 200) };
            bool? isGivenBack = null;
            dlg2.Shown += (s, e) =>
            {
                var inner = YANLoader.Show(dlg2, new YANLoaderOptions { ShowDelay = 5000 });
                inner.Dispose();
                isGivenBack = IsEnabled(dlg2);
                dlg2.Close();
            };
            _ = dlg2.ShowDialog(owner);
            Assert.True(isGivenBack ?? false, "a scope on a modal dialog waited for that dialog");
        });

        // The owner takes no keys and no wheel turns while the scope is open, also before the screen appears: a disabled window keeps
        // its focused child, and WinForms would still turn the keys into clicks (AcceptButton, the focused button) or text. The keys
        // of other windows are not touched, and the owner takes keys again once the scope is closed
        [Fact]
        public void Keys_DoNotReachTheOwner_WhileTheScopeIsOpen() => Run(() =>
        {
            const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_CHAR = 0x0102, WM_SYSKEYDOWN = 0x0104, WM_MOUSEWHEEL = 0x020A;
            const int VK_RETURN = 0x0D, VK_SPACE = 0x20, VK_A = 0x41, VK_F4 = 0x73;
            using var owner = ShowOwner();
            using var other = ShowOwner();
            var btn = new Button { Text = "Load", Location = new Point(10, 10) };
            var txt = new TextBox { Location = new Point(10, 50) };
            var otherTxt = new TextBox { Location = new Point(10, 10) };
            owner.Controls.Add(btn);
            owner.Controls.Add(txt);
            other.Controls.Add(otherTxt);
            owner.AcceptButton = btn;
            var seen = new List<string>();
            var clicks = 0;
            var closings = 0;
            btn.Click += (s, e) => clicks++;
            btn.KeyDown += (s, e) => seen.Add("button " + e.KeyCode);
            txt.KeyDown += (s, e) => seen.Add("text box " + e.KeyCode);
            txt.KeyPress += (s, e) => seen.Add("text box char");
            txt.MouseWheel += (s, e) => seen.Add("text box wheel");
            owner.KeyDown += (s, e) => seen.Add("form " + e.KeyCode);
            owner.FormClosing += (s, e) =>
            {
                closings++;
                e.Cancel = true;
            };
            var otherKeys = 0;
            otherTxt.KeyDown += (s, e) => otherKeys++;
            PumpFor(50);
            var scope = YANLoader.Show(owner, new YANLoaderOptions { ShowDelay = 5000 });
            PostKey(btn, WM_KEYDOWN, VK_RETURN, 1);
            PostKey(btn, WM_KEYUP, VK_RETURN, unchecked((int)0xC0000001));
            PostKey(btn, WM_KEYDOWN, VK_SPACE, 1);
            PostKey(btn, WM_KEYUP, VK_SPACE, unchecked((int)0xC0000001));
            PostKey(txt, WM_KEYDOWN, VK_A, 1);
            PostKey(txt, WM_CHAR, 'a', 1);
            PostKey(txt, WM_MOUSEWHEEL, -120 << 16, 0);
            // Alt+F4 to the owner itself (its keys when it is active without a focus)
            PostKey(owner, WM_SYSKEYDOWN, VK_F4, 0x20000001);
            PostKey(otherTxt, WM_KEYDOWN, VK_A, 1);
            Assert.True(PumpUntil(() => otherKeys == 1), "a key of another form was dropped");
            PumpFor(100);
            Assert.True(seen.Count == 0, "keys reached the blocked owner: " + string.Join(", ", seen));
            Assert.Equal(0, clicks);
            Assert.Equal(0, closings);
            Assert.Equal("", txt.Text);
            Assert.False(scope.Screen.Visible);
            scope.Dispose();
            Assert.True(IsEnabled(owner));
            // given back: the keys reach the owner again
            PostKey(txt, WM_KEYDOWN, VK_A, 1);
            Assert.True(PumpUntil(() => seen.Contains("text box A")), "the owner took no keys after the scope: " + string.Join(", ", seen));
            if (IsNativeInput)
            {
                // Windows: Enter clicks the AcceptButton (Form.ProcessDialogKey)
                PostKey(btn, WM_KEYDOWN, VK_RETURN, 1);
                PostKey(btn, WM_KEYUP, VK_RETURN, unchecked((int)0xC0000001));
                Assert.True(PumpUntil(() => clicks == 1), "Enter did not click the AcceptButton after the scope");
            }
        });

        // A scope opened in Load disables the form before Windows first shows and activates it (a disabled window takes no focus and
        // may not be activated at all). When the work ends before the screen appears, the form still ends up active and focused, as it
        // does without a scope
        [Fact]
        public void ScopeOpenedInLoad_FastWork_LeavesTheFormActiveAndFocused() => Run(() =>
        {
            using var main = ShowOwner();
            main.Activate();
            PumpFor(50);
            // whether this session can activate a window at all (a desktop without input may not)
            var canActivate = Form.ActiveForm == main;
            using var owner = NewOwner();
            var txt = new TextBox { Location = new Point(10, 10) };
            owner.Controls.Add(txt);
            Task task = null;
            var isBlocked = false;
            owner.Load += (s, e) => task = owner.RunWithLoaderAsync((p, ct) =>
            {
                isBlocked = !IsEnabled(owner) && ((YANLoaderScope)p).IsOwnerBlocked;
                return Task.Delay(10, ct);
            });
            owner.Show();
            Assert.NotNull(task);
            Complete(task);
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
            Assert.True(isBlocked, "the form took input while the work ran");
            Assert.True(IsEnabled(owner), "the form was not given back");
            if (IsNativeInput && canActivate)
            {
                Assert.True(PumpUntil(() => Form.ActiveForm == owner), "the form was not activated: the active form is " + Form.ActiveForm?.Text);
                Assert.True(PumpUntil(() => txt.Focused), "the form's first control has no keyboard focus");
            }
        });

        // An MDI child: the screen follows it when the MDI parent moves (the child's own Location does not change then)
        [Fact]
        public void Screen_FollowsTheParentOfAnMdiChild() => Run(() =>
        {
            using var parent = ShowOwner();
            parent.IsMdiContainer = true;
            using var child = new Form { MdiParent = parent, StartPosition = Manual, Bounds = new Rectangle(20, 10, 300, 200) };
            child.Show();
            PumpFor(50);
            var scope = YANLoader.Show(child, new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0 });
            var scr = scope.Screen;
            Assert.True(PumpUntil(() => scr.Visible), "not shown over the MDI child");
            Assert.Equal(YANOverlayScreen.ScreenBoundsOf(child), scr.Bounds);
            parent.Location = new Point(parent.Left + 50, parent.Top + 30);
            Assert.True(PumpUntil(() => scr.Bounds == YANOverlayScreen.ScreenBoundsOf(child)), $"did not follow the MDI parent: {scr.Bounds} for {YANOverlayScreen.ScreenBoundsOf(child)}");
            scope.Dispose();
            Assert.True(PumpUntil(() => scr.IsDisposed));
            // no longer follows
            parent.Location = new Point(parent.Left - 50, parent.Top - 30);
            PumpFor(50);
        });

        // A form embedded in another (TopLevel = false): the screen follows it when its parent control or the host form moves
        [Fact]
        public void Screen_FollowsTheParentsOfAnEmbeddedForm() => Run(() =>
        {
            using var host = ShowOwner();
            var panel = new Panel { Bounds = new Rectangle(30, 20, 400, 300) };
            host.Controls.Add(panel);
            var inner = new Form { TopLevel = false, FormBorderStyle = FormBorderStyle.None, Bounds = new Rectangle(10, 10, 200, 150) };
            panel.Controls.Add(inner);
            inner.Show();
            PumpFor(50);
            var scope = YANLoader.Show(inner, new YANLoaderOptions { ShowDelay = 0, FadeDuration = 0 });
            var scr = scope.Screen;
            Assert.True(PumpUntil(() => scr.Visible), "not shown over the embedded form");
            Assert.Equal(YANOverlayScreen.ScreenBoundsOf(inner), scr.Bounds);
            panel.Location = new Point(panel.Left + 25, panel.Top + 15);
            Assert.True(PumpUntil(() => scr.Bounds == YANOverlayScreen.ScreenBoundsOf(inner)), $"did not follow the panel: {scr.Bounds} for {YANOverlayScreen.ScreenBoundsOf(inner)}");
            host.Location = new Point(host.Left + 40, host.Top + 20);
            Assert.True(PumpUntil(() => scr.Bounds == YANOverlayScreen.ScreenBoundsOf(inner)), $"did not follow the host form: {scr.Bounds} for {YANOverlayScreen.ScreenBoundsOf(inner)}");
            scope.Dispose();
            Assert.True(PumpUntil(() => scr.IsDisposed));
        });

        // The bounds covered: the whole form, or on Windows its visible frame (without the invisible resize borders)
        [Fact]
        public void ScreenBounds_AreTheVisibleFrameOfTheOwner() => Run(() =>
        {
            using var owner = ShowOwner();
            var bounds = YANOverlayScreen.ScreenBoundsOf(owner);
            Assert.True(owner.Bounds.Contains(bounds) && bounds.Width > owner.Width / 2 && bounds.Height > owner.Height / 2, $"{bounds} for {owner.Bounds}");
            owner.FormBorderStyle = FormBorderStyle.None;
            Assert.True(PumpUntil(() => YANOverlayScreen.ScreenBoundsOf(owner) == owner.Bounds), $"{YANOverlayScreen.ScreenBoundsOf(owner)} for a borderless {owner.Bounds}");
        });
    }

    /// <summary>
    /// Helpers for the YANLoader tests: a UI thread with a working synchronization context, message pumping, and a stand-in for
    /// the user32 input calls where they do not exist.
    /// </summary>
    internal static class LoaderKit
    {
        #region Fields
        private const string OWNER_TEXT = "YANLoader owner";
        private static readonly bool _is_Mono = Type.GetType("Mono.Runtime") != null;
        private static bool _is_NativeInput;
        #endregion

        #region Properties
        /// <summary>
        /// The test runs with the real user32 input calls (Windows), not with their stand-in.
        /// </summary>
        public static bool IsNativeInput => _is_NativeInput;
        #endregion

        #region Methods
        /// <summary>
        /// Runs a loader test on an STA thread, then requires every loader screen to be closed.
        /// </summary>
        public static void Run(Action body) => Sta.Run(() =>
        {
            var oldInput = YANLoaderScope.Native;
            var oldAutoInstall = WindowsFormsSynchronizationContext.AutoInstall;
            var oldContext = SynchronizationContext.Current;
            var oldCheck = Control.CheckForIllegalCrossThreadCalls;
            var oldAnimate = YANOverlayScreen.CanAnimateOverride;
            Control marshal = null;
            try
            {
                _is_NativeInput = HasUser32(oldInput);
                if (!_is_NativeInput)
                {
                    YANLoaderScope.Native = new FakeInput();
                }
                if (_is_Mono)
                {
                    // mono's WindowsFormsSynchronizationContext posts through one static control of the first WinForms thread of the
                    // process, so await continuations on any later thread are lost: post through a control of this thread instead
                    WindowsFormsSynchronizationContext.AutoInstall = false;
                    marshal = new Control();
                    marshal.CreateControl();
                    _ = marshal.Handle;
                    SynchronizationContext.SetSynchronizationContext(new ControlContext(marshal));
                    // it brings the fades' continuations back to this thread like the WinForms context does
                    YANOverlayScreen.CanAnimateOverride = true;
                }
                else
                {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                }
                // mono's X11 driver itself reads windows of other threads while pumping (see skip.txt): check only on .NET
                Control.CheckForIllegalCrossThreadCalls = !_is_Mono;
                body();
                Assert.True(PumpUntil(() => OpenScreens().Length == 0), "loader screen still open: " + string.Join(", ", OpenScreens().Select(f => f.GetType().Name)));
            }
            finally
            {
                // Never leave a window behind, even after a failure (mono's ShowDialog touches the open forms of every thread)
                foreach (var frm in LeftOver())
                {
                    frm.Dispose();
                }
                Control.CheckForIllegalCrossThreadCalls = oldCheck;
                YANOverlayScreen.CanAnimateOverride = oldAnimate;
                SynchronizationContext.SetSynchronizationContext(oldContext);
                WindowsFormsSynchronizationContext.AutoInstall = oldAutoInstall;
                YANLoaderScope.Native = oldInput;
                marshal?.Dispose();
            }
        });

        /// <summary>
        /// A normal owner form (not top-most) at a known place, not shown yet.
        /// </summary>
        public static Form NewOwner() => new()
        {
            ShowInTaskbar = false,
            StartPosition = Manual,
            Bounds = new Rectangle(100, 80, 640, 480),
            Text = OWNER_TEXT
        };

        /// <summary>
        /// A shown, normal owner form (not top-most) at a known place.
        /// </summary>
        public static Form ShowOwner()
        {
            var frm = NewOwner();
            try
            {
                frm.Show();
                PumpFor(50);
                return frm;
            }
            catch
            {
                frm.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Whether the owner window takes input (through the same user32 calls, or their stand-in, as the library).
        /// </summary>
        public static bool IsEnabled(Form frm) => YANLoaderScope.Native.IsWindowEnabled(frm.Handle);

        /// <summary>
        /// Expected screen bounds: over the owner's visible frame (Load, Wait) or centred on it inside the working area (Update).
        /// </summary>
        public static Rectangle Expected(YANLoaderKind kind, Form owner, Form scr)
        {
            var frame = YANOverlayScreen.ScreenBoundsOf(owner);
            if (kind != Update)
            {
                return frame;
            }
            var area = System.Windows.Forms.Screen.FromRectangle(frame).WorkingArea;
            var x = Math.Max(area.Left, Math.Min((frame.Left + frame.Right - scr.Width) / 2, area.Right - scr.Width));
            var y = Math.Max(area.Top, Math.Min((frame.Top + frame.Bottom - scr.Height) / 2, area.Bottom - scr.Height));
            return new Rectangle(x, y, scr.Width, scr.Height);
        }

        /// <summary>
        /// The form has a region built for its current size (a GDI round-rect region may be one pixel short on the right and bottom).
        /// </summary>
        public static void AssertRegionFits(Form frm)
        {
            Assert.NotNull(frm.Region);
            using var g = frm.CreateGraphics();
            var b = frm.Region.GetBounds(g);
            Assert.True(b.X == 0 && b.Y == 0 && Math.Abs(b.Width - frm.Width) <= 1 && Math.Abs(b.Height - frm.Height) <= 1, $"region {b} for a {frm.Size} window");
        }

        /// <summary>
        /// Loader screens still open.
        /// </summary>
        public static Form[] OpenScreens() => Application.OpenForms.OfType<YANOverlayScreen>().Where(f => !f.IsDisposed).ToArray<Form>();

        // Owners and screens of this thread that are still open
        private static Form[] LeftOver() => Application.OpenForms.Cast<Form>().Where(f => (f is YANOverlayScreen || f.Text == OWNER_TEXT) && !f.IsDisposed && f.IsHandleCreated && !f.InvokeRequired).ToArray();

        /// <summary>
        /// Pumps messages until the condition holds (true) or the timeout elapses (false).
        /// </summary>
        public static bool PumpUntil(Func<bool> condition, int timeoutMs = Poll.TIMEOUT_MS)
        {
            var sw = Stopwatch.StartNew();
            while (!condition())
            {
                if (sw.ElapsedMilliseconds > timeoutMs)
                {
                    return condition();
                }
                Application.DoEvents();
                Thread.Sleep(5);
            }
            return true;
        }

        /// <summary>
        /// Pumps messages for a while.
        /// </summary>
        public static void PumpFor(int ms)
        {
            var sw = Stopwatch.StartNew();
            _ = PumpUntil(() => sw.ElapsedMilliseconds >= ms, ms + 1000);
        }

        /// <summary>
        /// Pumps messages until the task has ended.
        /// </summary>
        public static void Complete(Task task) => Assert.True(PumpUntil(() => task.IsCompleted), "the loader task did not end");

        /// <summary>
        /// Posts an input message to a window of this thread, as the keyboard or the mouse wheel does: through user32, or through mono's
        /// own message queue where there is none. It goes through the message filters and WinForms' key processing when pumped.
        /// </summary>
        public static void PostKey(Control target, int msg, int wParam, int lParam)
        {
            if (!_is_Mono)
            {
                Assert.True(PostMessageW(target.Handle, msg, (IntPtr)wParam, (IntPtr)lParam), "PostMessage failed");
                return;
            }
            var swf = typeof(Control).Assembly;
            var msgType = swf.GetType("System.Windows.Forms.Msg", true);
            var post = swf.GetType("System.Windows.Forms.XplatUI", true).GetMethod("PostMessage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(IntPtr), msgType, typeof(IntPtr), typeof(IntPtr) }, null);
            _ = post.Invoke(null, new object[] { target.Handle, Enum.ToObject(msgType, msg), (IntPtr)wParam, (IntPtr)lParam });
        }

        /// <summary>
        /// Runs the action on a worker thread while this (UI) thread keeps pumping messages.
        /// </summary>
        public static void OnWorker(Action action)
        {
            Exception error = null;
            var worker = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            worker.Start();
            Assert.True(PumpUntil(() => !worker.IsAlive), "the worker thread hung");
            Assert.Null(error);
        }

        /// <summary>
        /// The call throws at once (not through a faulted task) an exception of type <typeparamref name="T"/> for the parameter.
        /// </summary>
        public static void ThrowsNow<T>(string paramName, Action call) where T : ArgumentException
        {
            var ex = Assert.IsType<T>(Record(call));
            Assert.Equal(paramName, ex.ParamName);
        }

        /// <summary>
        /// The exception thrown by the action, or null.
        /// </summary>
        public static Exception Record(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        [DllImport("user32.dll", EntryPoint = "PostMessageW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // user32 is there (Windows) when the real calls work
        private static bool HasUser32(YANLoaderScope.IWindowInput input)
        {
            try
            {
                _ = input.IsWindowEnabled(IntPtr.Zero);
                return true;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                return false;
            }
        }
        #endregion

        #region Nested types
        /// <summary>
        /// Stand-in for the user32 input calls (mono has no user32): remembers which windows are disabled.
        /// </summary>
        private sealed class FakeInput : YANLoaderScope.IWindowInput
        {
            private readonly HashSet<IntPtr> _disabled = new();

            public bool IsWindowEnabled(IntPtr hWnd)
            {
                lock (_disabled)
                {
                    return !_disabled.Contains(hWnd);
                }
            }

            public bool EnableWindow(IntPtr hWnd, bool enable)
            {
                lock (_disabled)
                {
                    var wasDisabled = _disabled.Contains(hWnd);
                    if (enable)
                    {
                        _ = _disabled.Remove(hWnd);
                    }
                    else
                    {
                        _ = _disabled.Add(hWnd);
                    }
                    return wasDisabled;
                }
            }

            public IntPtr GetFocus() => IntPtr.Zero;

            public void SetFocus(IntPtr hWnd)
            {
            }

            // Through the WinForms parents (every window of the tests is a control)
            public bool IsChild(IntPtr hWndParent, IntPtr hWnd)
            {
                for (var c = Control.FromHandle(hWnd)?.Parent; c != null; c = c.Parent)
                {
                    if (c.IsHandleCreated && c.Handle == hWndParent)
                    {
                        return true;
                    }
                }
                return false;
            }

            // Forms are found through Application.OpenForms; there are no other windows
            public IntPtr[] GetThreadWindows() => new IntPtr[0];

            public bool IsWindowVisible(IntPtr hWnd) => Control.FromHandle(hWnd) is { Visible: true };

            public IntPtr GetOwner(IntPtr hWnd) => IntPtr.Zero;

            public bool IsToolWindow(IntPtr hWnd) => false;

            public IntPtr GetActiveWindow() => IntPtr.Zero;
        }

        /// <summary>
        /// Adds stand-ins for windows of the UI thread that are not forms (a native message box, a tooltip) to the real or stand-in
        /// user32 calls. The stand-ins are visible until closed.
        /// </summary>
        public sealed class ExtraWindowsInput : YANLoaderScope.IWindowInput
        {
            private readonly YANLoaderScope.IWindowInput _inner;
            private readonly Dictionary<IntPtr, (IntPtr Owner, bool IsToolWindow, bool IsEnabled)> _windows = new();
            private int _next = 0x7FFF0000;

            public ExtraWindowsInput(YANLoaderScope.IWindowInput inner) => _inner = inner;

            /// <summary>
            /// Stand-in reported as the active window when not zero.
            /// </summary>
            public IntPtr Active { get; set; }

            public IntPtr Open(IntPtr owner, bool isToolWindow = false, bool isEnabled = true)
            {
                var hwnd = new IntPtr(_next++);
                _windows.Add(hwnd, (owner, isToolWindow, isEnabled));
                return hwnd;
            }

            public void Close(IntPtr hwnd) => _windows.Remove(hwnd);

            public bool IsWindowEnabled(IntPtr hWnd) => _windows.TryGetValue(hWnd, out var w) ? w.IsEnabled : _inner.IsWindowEnabled(hWnd);

            public bool EnableWindow(IntPtr hWnd, bool enable) => _inner.EnableWindow(hWnd, enable);

            public IntPtr GetFocus() => _inner.GetFocus();

            public void SetFocus(IntPtr hWnd) => _inner.SetFocus(hWnd);

            public bool IsChild(IntPtr hWndParent, IntPtr hWnd) => _inner.IsChild(hWndParent, hWnd);

            public IntPtr[] GetThreadWindows() => _inner.GetThreadWindows().Concat(_windows.Keys).ToArray();

            public bool IsWindowVisible(IntPtr hWnd) => _windows.ContainsKey(hWnd) || _inner.IsWindowVisible(hWnd);

            public IntPtr GetOwner(IntPtr hWnd) => _windows.TryGetValue(hWnd, out var w) ? w.Owner : _inner.GetOwner(hWnd);

            public bool IsToolWindow(IntPtr hWnd) => _windows.TryGetValue(hWnd, out var w) ? w.IsToolWindow : _inner.IsToolWindow(hWnd);

            public IntPtr GetActiveWindow() => Active != IntPtr.Zero ? Active : _inner.GetActiveWindow();
        }

        /// <summary>
        /// Posts through a control of the UI thread (what WindowsFormsSynchronizationContext does on .NET Framework).
        /// </summary>
        private sealed class ControlContext : SynchronizationContext
        {
            private readonly Control _control;

            public ControlContext(Control control) => _control = control;

            public override void Post(SendOrPostCallback d, object state) => _control.BeginInvoke(d, new[] { state });

            public override void Send(SendOrPostCallback d, object state) => _control.Invoke(d, new[] { state });

            public override SynchronizationContext CreateCopy() => this;
        }
        #endregion
    }
}
