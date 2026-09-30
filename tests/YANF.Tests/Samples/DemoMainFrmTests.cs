using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Xunit;
using YANF.Control;
using YANF.Demo;
using YANF.Screen;
using static System.Windows.Forms.FormStartPosition;
using static YANF.Tests.Services.LoaderKit;

namespace YANF.Tests.Samples
{
    // The main demo form (1.1): its Update, Wait and Load buttons use YANLoader; the "Use 1.0 services (legacy)" switch keeps one
    // example of each 1.0 service (IYANSrcService: Wait; IYANDlvScrService: Load and Update). 1.0 drove them with a 100 ms timer
    // through a string state machine and faded the form out and in with the blocking FadeOut/FadeIn
    public class DemoMainFrmTests
    {
        #region Fields
        // The simulated work takes 100 steps of 30 ms
        private const int WORK_MS = 3000;
        private const int DONE_TIMEOUT_MS = 20000;
        #endregion

        #region YANLoader
        // Load: RunWithLoaderAsync with the work on the thread pool; the percentage it reports reaches the screen
        [Fact]
        public void Load_RunsWithLoader_AndShowsTheProgress() => Run(() =>
        {
            var percents = RunButton("BtnLoadScr_Click", typeof(YANLoadScreen), scr => Part(scr, "lblPercent"));
            AssertProgress(percents);
        });

        // Update: a YANLoader.Show scope around awaited work; SetProgress shows the percentage and the detail text
        [Fact]
        public void Update_RunsInALoaderScope_AndShowsTheProgressAndTheDetail() => Run(() =>
        {
            var details = RunButton("BtnUpdScr_Click", typeof(YANUpdateScreen), scr => Part(scr, "lblPercent") + "|" + Part(scr, "lblCapacity"));
            AssertProgress(details.Select(d => d.Split('|')[0]).ToList());
            var capacities = details.Select(d => d.Split('|')[1]).Where(c => c.Length > 0).ToList();
            Assert.NotEmpty(capacities);
            Assert.All(capacities, c => Assert.Matches(new Regex(@"^\d+([.,]\d{1,2})? MB / 137 MB$"), c));
        });

        // Wait: RunWithLoaderAsync with a plain task and the Wait screen
        [Fact]
        public void Wait_RunsWithLoader() => Run(() => RunButton("BtnWaitScr_Click", typeof(YANWaitScreen), _ => null));
        #endregion

        #region 1.0 services
        // The switch is off by default and its label switches it too
        [Fact]
        public void LegacySwitch_IsOffByDefault_AndItsLabelSwitchesIt() => Sta.Run(() =>
        {
            using var frm = new MainFrm();
            var tg = Priv.Field<YANTg>(frm, "tgLegacy");
            var lbl = Priv.Field<Label>(frm, "lblLegacy");
            Assert.False(tg.Checked);
            Assert.Contains("1.0", lbl.Text);
            Priv.Call(frm, "LblLegacy_Click", lbl, EventArgs.Empty);
            Assert.True(tg.Checked);
            Priv.Call(frm, "LblLegacy_Click", lbl, EventArgs.Empty);
            Assert.False(tg.Checked);
        });

        // Switched on, each button runs its 1.0 service around work that blocks the UI thread: the screen runs on a thread of its
        // own meanwhile and shows the progress that PublishValue sends from the blocked thread (Load, Update), the form is not
        // disabled (no YANLoader), and the screen is gone afterwards
        [Theory]
        [InlineData("BtnLoadScr_Click", typeof(YANLoadScreen), true)]
        [InlineData("BtnWaitScr_Click", typeof(YANWaitScreen), false)]
        [InlineData("BtnUpdScr_Click", typeof(YANUpdateScreen), true)]
        public void LegacySwitch_UsesThe10Services(string handler, Type screen, bool hasProgress) => Run(() =>
        {
            using var frm = ShowMainFrm();
            using var boxes = new BoxWatcher(box => box.Hide());
            Priv.Field<YANTg>(frm, "tgLegacy").Checked = true;
            var seen = new ConcurrentDictionary<Type, bool>();
            var maxPercent = -1;
            var isWatching = true;
            // the screen lives on another thread: watch the open forms from a third one while this thread is blocked
            var watcher = new Thread(() =>
            {
                while (Volatile.Read(ref isWatching))
                {
                    try
                    {
                        foreach (var f in Application.OpenForms.Cast<Form>().ToArray())
                        {
                            if (f is YANOverlayScreen && f.Visible)
                            {
                                // a 1.0 screen has no owner (it never gets one on another thread); a YANLoader screen is owned
                                seen[f.GetType()] = f.Owner == null;
                                if (hasProgress && ReadOn(f, () => Part(f, "lblPercent")) is { Length: > 1 } text)
                                {
                                    maxPercent = Math.Max(maxPercent, int.Parse(text.TrimEnd('%')));
                                }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
                    {
                        // the collection changed while it was read, or the screen closed meanwhile: read again
                    }
                    Thread.Sleep(10);
                }
            })
            {
                IsBackground = true
            };
            watcher.Start();
            var sw = Stopwatch.StartNew();
            try
            {
                Priv.Call(frm, handler, frm, EventArgs.Empty);
            }
            finally
            {
                Volatile.Write(ref isWatching, false);
                watcher.Join();
            }
            Assert.True(sw.ElapsedMilliseconds >= WORK_MS * 8 / 10, $"returned after {sw.ElapsedMilliseconds} ms: the work did not block the UI thread");
            Assert.True(seen.TryGetValue(screen, out var isOwnerless), $"no {screen.Name} was shown: " + string.Join(", ", seen.Keys.Select(t => t.Name)));
            Assert.True(isOwnerless, "a YANLoader screen was shown instead of the 1.0 one");
            Assert.Single(seen);
            if (hasProgress)
            {
                Assert.InRange(maxPercent, 50, 100);
            }
            Assert.True(IsEnabled(frm), "the form was disabled");
            Assert.True(PumpUntil(() => OpenScreens().Length == 0), "the 1.0 screen was not closed");
            PumpFor(50);
            boxes.Check(0);
        });
        #endregion

        #region Methods
        // The main form at a known place (it is shown, so YANLoader can block it)
        private static MainFrm ShowMainFrm()
        {
            var frm = new MainFrm
            {
                StartPosition = Manual,
                Location = Point.Empty,
                ShowInTaskbar = false
            };
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

        // Clicks a YANLoader button: the form is blocked at once, the screen appears owned by it and is gone when the work ends,
        // and the form takes input again. Returns what read showed on the screen while it was up (distinct values, in order)
        private static List<string> RunButton(string handler, Type screen, Func<YANOverlayScreen, string> read)
        {
            using var frm = ShowMainFrm();
            // a failed run would show an error box (the handlers catch everything)
            using var boxes = new BoxWatcher(box => box.Hide());
            var values = new List<string>();
            var sw = Stopwatch.StartNew();
            // an async void handler: returns at its first await
            Priv.Call(frm, handler, frm, EventArgs.Empty);
            Assert.False(IsEnabled(frm), "the form took input while the work ran");
            YANOverlayScreen scr = null;
            Assert.True(PumpUntil(() => (scr = OpenScreens().OfType<YANOverlayScreen>().FirstOrDefault(s => s.Visible)) != null), "no screen was shown");
            Assert.IsType(screen, scr);
            Assert.Same(frm, scr.Owner);
            Assert.True(PumpUntil(() =>
            {
                if (!scr.IsDisposed && read(scr) is { } value && (values.Count == 0 || values[values.Count - 1] != value))
                {
                    values.Add(value);
                }
                return scr.IsDisposed && IsEnabled(frm);
            }, DONE_TIMEOUT_MS), "the screen did not close or the form was not given back");
            Assert.True(sw.ElapsedMilliseconds >= WORK_MS * 8 / 10, $"done after {sw.ElapsedMilliseconds} ms");
            Assert.Empty(OpenScreens());
            boxes.Check(0);
            return values;
        }

        // Percentages ("37%") that only go up and pass the middle
        private static void AssertProgress(List<string> percents)
        {
            var values = percents.Where(p => p.Length > 0).Select(p =>
            {
                Assert.EndsWith("%", p);
                return int.Parse(p.TrimEnd('%'));
            }).ToList();
            Assert.True(values.Count >= 5, "progress shown: " + string.Join(", ", percents));
            Assert.True(values.Zip(values.Skip(1), (a, b) => a < b).All(x => x), "the progress went back: " + string.Join(", ", values));
            Assert.InRange(values.Last(), 50, 100);
        }

        // Text of a named part of a screen
        private static string Part(Form scr, string name) => Priv.Field<System.Windows.Forms.Control>(scr, name).Text;

        // Runs read on the thread of the form (null when it does not answer within half a second)
        private static string ReadOn(Form frm, Func<string> read)
        {
            var call = frm.BeginInvoke(read);
            return call.AsyncWaitHandle.WaitOne(500) ? (string)frm.EndInvoke(call) : null;
        }
        #endregion
    }
}
