using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit;
using YANF.Script;
using static System.Windows.Forms.DialogResult;

namespace YANF.Tests.Script
{
    using YANPaint = YANF.Control.YANPaint;

    // HighLightLblLinkByCtrl: 1.0.2 created a new Font on every call (GDI leak on every Enter/Leave), threw for short names,
    // parentless controls and same-named non-Label controls, and dropped the label's other style bits
    public class YANDisplayTests
    {
        [Fact]
        public void HighLight_AllocatesFontOnlyWhenBoldChanges() => Sta.Run(ui =>
        {
            using var frm = new Form();
            using var consumerFont = new Font("Arial", 10f, FontStyle.Italic);
            var txt = new TextBox { Name = "txtName" };
            var lbl = new Label { Name = "lblName", Font = consumerFont };
            frm.Controls.Add(txt);
            frm.Controls.Add(lbl);
            // the demo pattern: isBold = false on an already regular label
            for (var i = 0; i < 50; i++)
            {
                txt.HighLightLblLinkByCtrl("txt", i % 2 == 0 ? Color.LightYellow : Color.WhiteSmoke, false);
                Assert.Same(consumerFont, lbl.Font);
            }
            Assert.Equal(Color.WhiteSmoke, lbl.ForeColor);
            // bold on: one new font, other style bits kept, consumer font untouched
            txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
            var bold = lbl.Font;
            Assert.NotSame(consumerFont, bold);
            Assert.True(bold.Bold && bold.Italic, "style bits lost: " + bold.Style);
            Assert.Equal(consumerFont.Name, bold.Name);
            Assert.Equal(consumerFont.Size, bold.Size);
            Assert.False(Gdi.IsDisposed(consumerFont), "consumer font disposed");
            Assert.Equal(Color.Red, lbl.ForeColor);
            // bold again: no allocation
            for (var i = 0; i < 10; i++)
            {
                txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
                Assert.Same(bold, lbl.Font);
            }
            // bold off: italic kept, the library-created bold font released, the consumer font still alive
            txt.HighLightLblLinkByCtrl("txt", Color.WhiteSmoke, false);
            var regular = lbl.Font;
            Assert.True(!regular.Bold && regular.Italic, "unbold style: " + regular.Style);
            Assert.NotSame(consumerFont, regular);
            Assert.True(Gdi.IsDisposed(bold), "the library-created bold font was not disposed when replaced");
            Assert.False(Gdi.IsDisposed(consumerFont), "consumer font disposed");
            Assert.False(Gdi.IsDisposed(regular), "current font disposed");
            ui.Draw(lbl);
        });

        [Fact]
        public void HighLight_NeverDisposesInheritedOrConsumerFonts() => Sta.Run(() =>
        {
            using var frm = new Form { Font = new Font("Arial", 9f, FontStyle.Underline) };
            var formFont = frm.Font;
            var pnl = new Panel();
            var dp = new DateTimePicker { Name = "dpBirth" };
            var lbl = new Label { Name = "lblBirth" };
            pnl.Controls.Add(lbl);
            frm.Controls.Add(pnl);
            frm.Controls.Add(dp);
            Assert.Same(formFont, lbl.Font);
            dp.HighLightLblLinkByCtrl("dp", Color.Yellow, false);
            Assert.Same(formFont, lbl.Font);
            dp.HighLightLblLinkByCtrl("dp", Color.Yellow, true);
            Assert.True(lbl.Font.Bold && lbl.Font.Underline, "bold + underline expected: " + lbl.Font.Style);
            Assert.False(Gdi.IsDisposed(formFont), "inherited form font disposed");
            Assert.Same(formFont, frm.Font);
            // a consumer font set between the calls is never disposed either
            using var consumerFont = new Font("Arial", 12f, FontStyle.Bold);
            lbl.Font = consumerFont;
            dp.HighLightLblLinkByCtrl("dp", Color.Yellow, false);
            Assert.False(lbl.Font.Bold);
            Assert.Equal(12f, lbl.Font.Size);
            Assert.False(Gdi.IsDisposed(consumerFont), "consumer font disposed");
            Assert.False(Gdi.IsDisposed(formFont), "form font disposed");
        });

        [Fact]
        public void HighLight_Guards() => Sta.Run(() =>
        {
            // not on a form
            using var lone = new TextBox { Name = "txtLone" };
            lone.HighLightLblLinkByCtrl("txt", Color.Red, true);

            using var frm = new Form();
            var shortName = new TextBox { Name = "tx" };
            var noName = new TextBox();
            var txt = new TextBox { Name = "txtCity" };
            var notLabel = new Button { Name = "lblCity", Text = "b" };
            frm.Controls.AddRange(new System.Windows.Forms.Control[] { shortName, noName, txt, notLabel });
            var btnFont = notLabel.Font;
            shortName.HighLightLblLinkByCtrl("txt", Color.Red, true);
            noName.HighLightLblLinkByCtrl("txt", Color.Red, true);
            txt.HighLightLblLinkByCtrl(null, Color.Red, true);
            ((System.Windows.Forms.Control)null).HighLightLblLinkByCtrl("txt", Color.Red, true);
            // only a non-Label matches: nothing happens (1.0.2 threw InvalidCastException)
            txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
            Assert.NotEqual(Color.Red, notLabel.ForeColor);
            Assert.Same(btnFont, notLabel.Font);
            // a Label further down with the same name is found past the non-Label
            var inner = new Panel();
            var lbl = new Label { Name = "lblCity" };
            inner.Controls.Add(lbl);
            frm.Controls.Add(inner);
            txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
            Assert.Equal(Color.Red, lbl.ForeColor);
            Assert.True(lbl.Font.Bold);
        });
    

        // FadeToAsync / EnableFade: 1.0 only had FadeIn/FadeOut, which block the UI thread (Sleep loop) and, called from FormClosing,
        // left an invisible form when another handler cancelled the close. YANDisplay.UIEffectsOverride forces the animated path on
        // this test thread whatever the machine's animation setting is (mono and many CI sessions report animations off).

        [Fact]
        public void FadeToAsync_IsTimeBased_Eased_AndReachesTheTarget() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            var frm = ui.Show();
            var samples = new List<double>();
            var clock = Stopwatch.StartNew();
            var fade = frm.FadeToAsync(0.25, 300);
            // returns before the fade is done: the caller's thread is not blocked
            Assert.False(fade.IsCompleted);
            Assert.True(PumpUntil(() =>
            {
                samples.Add(frm.Opacity);
                return fade.IsCompleted;
            }), "the fade did not complete");
            clock.Stop();
            Assert.Equal(TaskStatus.RanToCompletion, fade.Status);
            Assert.Equal(0.25, frm.Opacity, 3);
            Assert.InRange(clock.ElapsedMilliseconds, 270, 3000);
            // several intermediate frames, moving one way only, never past the target
            Assert.True(samples.Distinct().Count() >= 4, "frames: " + samples.Distinct().Count());
            for (var i = 1; i < samples.Count; i++)
            {
                Assert.True(samples[i] <= samples[i - 1] + 1e-9, $"opacity went back up at frame {i}");
                Assert.InRange(samples[i], 0.25 - 1e-9, 1);
            }
            // and back up
            fade = frm.FadeToAsync(1, 100);
            Assert.True(PumpUntil(() => fade.IsCompleted));
            Assert.Equal(1, frm.Opacity, 3);
        });

        [Fact]
        public void FadeToAsync_WithoutAnimation_CompletesAtOnce_AndClamps() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            var frm = ui.Show();
            YANDisplay.UIEffectsOverride = true;
            var fade = frm.FadeToAsync(0.5, 0);
            Assert.Equal(TaskStatus.RanToCompletion, fade.Status);
            Assert.Equal(0.5, frm.Opacity, 3);
            Assert.True(frm.FadeToAsync(7, -10).IsCompleted);
            Assert.Equal(1, frm.Opacity, 3);
            Assert.True(frm.FadeToAsync(-3, 0).IsCompleted);
            Assert.Equal(0, frm.Opacity, 3);
            // Windows animation effects off: the target is applied at once whatever the duration
            YANDisplay.UIEffectsOverride = false;
            Assert.True(frm.FadeToAsync(0.6, 5000).IsCompleted);
            Assert.Equal(0.6, frm.Opacity, 3);
            // argument errors are thrown by the call itself
            Assert.Throws<ArgumentNullException>(() => { _ = ((Form)null).FadeToAsync(1, 100); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = frm.FadeToAsync(double.NaN, 100); });
        });

        [Fact]
        public void FadeToAsync_FormDisposedBeforeOrDuringTheFade_CompletesQuietly() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var gone = NewForm();
            gone.Dispose();
            Assert.Equal(TaskStatus.RanToCompletion, gone.FadeToAsync(0, 200).Status);

            using var frm = NewForm();
            frm.Show();
            frm.Opacity = 0.5;
            var fade = frm.FadeToAsync(1, 400);
            PumpFor(60);
            frm.Dispose();
            Assert.True(PumpUntil(() => fade.IsCompleted, 3000), "the fade did not stop");
            Assert.Equal(TaskStatus.RanToCompletion, fade.Status);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_StartsTransparent_AndFadesInToTheDesignedOpacity() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var frm = NewForm(0.8);
            frm.EnableFade(80, 60);
            // transparent before it is shown, so there is no flash at full opacity
            Assert.Equal(0, frm.Opacity, 3);
            var atShow = -1d;
            frm.VisibleChanged += (_, _) => atShow = frm.Opacity;
            frm.Show();
            Assert.InRange(atShow, 0, 0.2);
            Assert.True(PumpUntil(() => frm.Opacity >= 0.8 - 1e-6), "did not fade in: " + frm.Opacity);
            PumpFor(60);
            Assert.Equal(0.8, frm.Opacity, 3);

            // designed for FadeIn (opacity 0): fades in to 1
            using var legacy = NewForm(0);
            legacy.EnableFade(50, 50);
            legacy.Show();
            Assert.True(PumpUntil(() => legacy.Opacity >= 1 - 1e-6), "did not fade in: " + legacy.Opacity);

            // already on screen and transparent (EnableFade called from Shown): fades in now
            using var late = NewForm(0);
            late.Shown += (_, _) => late.EnableFade(50, 50);
            late.Show();
            Assert.True(PumpUntil(() => late.Opacity >= 1 - 1e-6), "did not fade in: " + late.Opacity);

            // fade-in turned off again before the form is shown: shown at its opacity, not left transparent
            using var changed = NewForm(0.7);
            changed.EnableFade(80, 60);
            changed.EnableFade(0, 60);
            changed.Show();
            Assert.Equal(0.7, changed.Opacity, 3);

            // animations off: nothing changes
            YANDisplay.UIEffectsOverride = false;
            using var plain = NewForm(0.9);
            plain.EnableFade(200, 200);
            Assert.Equal(0.9, plain.Opacity, 3);
            plain.Show();
            Assert.Equal(0.9, plain.Opacity, 3);
            plain.Close();
            Assert.True(plain.IsDisposed, "closed without a fade: disposed at once");

            // animations off and a form designed with Opacity 0 (for the blocking FadeIn): still shown, at full opacity
            using var designedTransparent = NewForm(0);
            designedTransparent.EnableFade(200, 200);
            designedTransparent.Show();
            Assert.Equal(1d, designedTransparent.Opacity, 3);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_HiddenDuringTheFadeIn_StopsAndFadesInAgainWhenShown() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var frm = NewForm();
            frm.EnableFade(400, 50);
            frm.Show();
            PumpFor(60);
            frm.Hide();
            var partial = frm.Opacity;
            Assert.InRange(partial, 0, 0.99);
            PumpFor(450);
            // no longer animating the hidden window
            Assert.Equal(partial, frm.Opacity, 6);
            frm.EnableFade(40, 50);
            frm.Show();
            Assert.True(PumpUntil(() => frm.Opacity >= 1 - 1e-6), "did not fade in again: " + frm.Opacity);
            // hidden with the fade-in turned off: shown at full opacity next time
            frm.EnableFade(400, 50);
            frm.Hide();
            frm.Show();
            PumpFor(40);
            frm.Hide();
            frm.EnableFade(0, 50);
            frm.Show();
            Assert.Equal(1, frm.Opacity, 3);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_NonModalClose_FadesOutThenCloses() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var frm = NewForm();
            frm.EnableFade(30, 150);
            var closing = 0;
            var closed = 0;
            var closedOpacity = -1d;
            frm.FormClosing += (_, _) => closing++;
            frm.FormClosed += (_, _) =>
            {
                closed++;
                closedOpacity = frm.Opacity;
            };
            frm.Show();
            Assert.True(PumpUntil(() => frm.Opacity >= 1 - 1e-6));
            frm.Close();
            // postponed: still open while it fades
            Assert.False(frm.IsDisposed);
            Assert.True(frm.Visible);
            // a second close request during the fade is absorbed by the pending one
            PumpFor(40);
            Assert.True(frm.Opacity < 1, "not fading: " + frm.Opacity);
            frm.Close();
            Assert.False(frm.IsDisposed);
            Assert.True(PumpUntil(() => frm.IsDisposed), "never closed");
            Assert.Equal(1, closed);
            Assert.Equal(0, closedOpacity, 3);
            // user close + absorbed request + the close after the fade
            Assert.Equal(3, closing);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_FormWithOwnedForms_ClosesWithoutTheFade() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var owner = NewForm();
            owner.EnableFade(20, 200);
            using var owned = NewForm();
            var ownedClosing = 0;
            owned.FormClosing += (_, _) => ownedClosing++;
            owner.AddOwnedForm(owned);
            owner.Show();
            owned.Show();
            Assert.True(PumpUntil(() => owner.Opacity >= 1 - 1e-6));
            // a postponed close would raise the owned form's FormClosing twice: no fade, closed at once
            owner.Close();
            Assert.True(owner.IsDisposed);
            // once on .NET Framework (FormOwnerClosing), never twice (mono does not raise it at all)
            Assert.InRange(ownedClosing, 0, 1);
            ui.ThrowIfFailed();
        });

        // Modal forms: mono raises FormClosing (and FormClosed) inside the DialogResult setter of a modal form, while .NET Framework
        // only stores the value and its modal loop raises them after the current message. Under mono these tests therefore do not
        // exercise the .NET Framework timing of the postponed close; they prove it only when they run on Windows (the CI job).
        [Fact]
        public void EnableFade_KeepsTheModalDialogResult() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            // DialogResult set as soon as the dialog is shown (during the fade-in)
            using (var dlg = NewForm())
            {
                dlg.EnableFade(60, 60);
                dlg.Shown += (_, _) => dlg.DialogResult = OK;
                Assert.Equal(OK, ShowDialogGuarded(dlg));
            }
            // DialogResult set once the dialog is fully shown: the close is postponed for the fade and the result survives it
            foreach (var expected in new[] { OK, Yes, No, Abort })
            {
                using var dlg = NewForm();
                dlg.EnableFade(30, 80);
                var closing = 0;
                var closedOpacity = -1d;
                dlg.FormClosing += (_, _) => closing++;
                dlg.FormClosed += (_, _) => closedOpacity = dlg.Opacity;
                using var closer = CloseWhenOpaque(dlg, () => dlg.DialogResult = expected);
                Assert.Equal(expected, ShowDialogGuarded(dlg));
                Assert.Equal(2, closing);
                Assert.Equal(0, closedOpacity, 3);
            }
            // closed with Close() (the X button): Cancel, as without the fade
            using (var dlg = NewForm())
            {
                dlg.EnableFade(30, 80);
                using var closer = CloseWhenOpaque(dlg, dlg.Close);
                Assert.Equal(Cancel, ShowDialogGuarded(dlg));
            }
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_ModalDialog_CanBeShownAgain() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            foreach (var fadeIn in new[] { 40, 0 })
            {
                using var dlg = NewForm();
                dlg.EnableFade(fadeIn, 60);
                var atShow = new List<double>();
                dlg.VisibleChanged += (_, _) =>
                {
                    if (dlg.Visible)
                    {
                        atShow.Add(dlg.Opacity);
                    }
                };
                for (var i = 0; i < 2; i++)
                {
                    // faded out by the previous close; must become visible again
                    using var closer = CloseWhenOpaque(dlg, () => dlg.DialogResult = OK);
                    Assert.Equal(OK, ShowDialogGuarded(dlg));
                    Assert.Equal(0, dlg.Opacity, 3);
                }
                Assert.Equal(2, atShow.Count);
                if (fadeIn > 0)
                {
                    Assert.All(atShow, o => Assert.InRange(o, 0, 0.2));
                }
            }
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_ModalSecondCloseCancelled_FadesBackIn_ThenCloses() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var dlg = NewForm();
            dlg.EnableFade(30, 60);
            var vetoes = 0;
            var closing = 0;
            var closed = 0;
            // added once the dialog is shown, so it runs after the fade handler and only sees the second close
            dlg.Shown += (_, _) => dlg.FormClosing += (_, e) =>
            {
                closing++;
                if (!e.Cancel && vetoes == 0)
                {
                    vetoes++;
                    e.Cancel = true;
                }
            };
            dlg.FormClosed += (_, _) => closed++;
            var step = 0;
            using var timer = new System.Windows.Forms.Timer { Interval = 10 };
            timer.Tick += (_, _) =>
            {
                if (!dlg.Visible || dlg.Opacity < 1 - 1e-6)
                {
                    return;
                }
                if (step == 0)
                {
                    step = 1;
                    dlg.DialogResult = OK;
                }
                else if (vetoes == 1)
                {
                    // faded back in after the veto (not left open and invisible): close it for real
                    timer.Stop();
                    dlg.DialogResult = Yes;
                }
            };
            timer.Start();
            Assert.Equal(Yes, ShowDialogGuarded(dlg));
            Assert.Equal(1, vetoes);
            // (first close, vetoed second close, second attempt, its postponed close) and a single FormClosed
            Assert.Equal(4, closing);
            Assert.Equal(1, closed);
            Assert.Equal(0, dlg.Opacity, 3);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_AtDesignTime_DoesNothing() => Sta.Run(ui =>
        {
            YANDisplay.UIEffectsOverride = true;
            using var frm = NewForm(0.8);
            var before = HandlerCounts(frm);
            var previous = System.ComponentModel.LicenseManager.CurrentContext;
            // what the Visual Studio designer sets while it runs the constructor of a base form
            System.ComponentModel.LicenseManager.CurrentContext = new System.ComponentModel.Design.DesigntimeLicenseContext();
            try
            {
                frm.EnableFade(40, 40);
            }
            finally
            {
                System.ComponentModel.LicenseManager.CurrentContext = previous;
            }
            Assert.Equal(0.8, frm.Opacity, 3);
            Assert.Equal(before, HandlerCounts(frm));
            // at run time the same call makes the form transparent until it is shown
            frm.EnableFade(40, 40);
            Assert.Equal(0, frm.Opacity, 3);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_Twice_SubscribesOnce() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var frm = NewForm();
            var before = HandlerCounts(frm);
            frm.EnableFade(40, 40);
            frm.EnableFade(40, 60);
            Assert.Equal(before.Select(n => n + 1), HandlerCounts(frm));
            frm.Show();
            frm.EnableFade(30, 60);
            Assert.Equal(before.Select(n => n + 1), HandlerCounts(frm));
            Assert.True(PumpUntil(() => frm.Opacity >= 1 - 1e-6));
            var closed = 0;
            frm.FormClosed += (_, _) => closed++;
            frm.Close();
            Assert.True(PumpUntil(() => frm.IsDisposed));
            Assert.Equal(1, closed);
            Assert.Throws<ArgumentNullException>(() => ((Form)null).EnableFade(1, 1));
            ui.ThrowIfFailed();
        });

        [Theory]
        [InlineData("before EnableFade")]
        [InlineData("in Load")]
        public void EnableFade_CloseCancelledByAnotherHandler_StaysCancelled(string when) => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var frm = NewForm();
            var cancel = true;
            var asked = 0;
            FormClosingEventHandler veto = (_, e) =>
            {
                asked++;
                e.Cancel = cancel;
            };
            if (when == "before EnableFade")
            {
                frm.FormClosing += veto;
            }
            else
            {
                frm.Load += (_, _) => frm.FormClosing += veto;
            }
            frm.EnableFade(30, 80);
            frm.Show();
            Assert.True(PumpUntil(() => frm.Opacity >= 1 - 1e-6));
            frm.Close();
            PumpFor(200);
            // no fade, no second close: the form stays as it was
            Assert.False(frm.IsDisposed);
            Assert.True(frm.Visible);
            Assert.Equal(1, frm.Opacity, 3);
            Assert.Equal(1, asked);
            // once the handler agrees, the form fades out and closes (the handler is asked again for the second close)
            cancel = false;
            frm.Close();
            Assert.True(PumpUntil(() => frm.IsDisposed));
            Assert.Equal(3, asked);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void EnableFade_SecondCloseCancelled_FadesBackIn() => Sta.Run(ui =>
        {
            using var context = UiContext.Install();
            YANDisplay.UIEffectsOverride = true;
            using var frm = NewForm();
            frm.EnableFade(30, 60);
            frm.Show();
            Assert.True(PumpUntil(() => frm.Opacity >= 1 - 1e-6));
            // added after the form is shown, so it runs after the fade handler and only sees the second close
            var asked = 0;
            frm.FormClosing += (_, e) =>
            {
                if (!e.Cancel)
                {
                    asked++;
                    e.Cancel = true;
                }
            };
            frm.Close();
            Assert.True(PumpUntil(() => asked == 1), "second close not raised");
            // not left invisible
            Assert.True(PumpUntil(() => frm.Opacity >= 1 - 1e-6), "left at opacity " + frm.Opacity);
            Assert.False(frm.IsDisposed);
            Assert.True(frm.Visible);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void SetRoundRegion_ZeroCorner_RemovesAndDisposesTheRegion() => Sta.Run(() =>
        {
            using var frm = new Form { FormBorderStyle = FormBorderStyle.None, Size = new Size(200, 100) };
            var old = new Region(new Rectangle(0, 0, 10, 10));
            frm.Region = old;
            frm.SetRoundRegion(0);
            Assert.Null(frm.Region);
            Assert.True(Gdi.IsDisposed(old), "the replaced region was not disposed");
            frm.SetRoundRegion(-5);
            Assert.Null(frm.Region);
            Assert.Throws<ArgumentNullException>(() => ((System.Windows.Forms.Control)null).SetRoundRegion(10));
        });

        [Fact]
        public void SetRoundRegion_Corner_ReplacesAndDisposesTheRegion() => Sta.Run(() =>
        {
            using var frm = new Form { FormBorderStyle = FormBorderStyle.None, Size = new Size(200, 100) };
            frm.SetRoundRegion(20);
            Assert.NotNull(frm.Region);
            var first = frm.Region;
            frm.SetRoundRegion(30);
            Assert.NotSame(first, frm.Region);
            Assert.True(Gdi.IsDisposed(first), "the replaced region was not disposed");
        });

        // The internal DPI helper of the screens forwards to the one of the controls (YANPaint): the control's DPI with the rounding of
        // LogicalToDeviceUnits where the runtime has it (.NET Framework 4.7 and later), the 96-dpi length itself where it has not (mono);
        // the test override (YANPaint.DpiOverride, shared with the controls) replaces the DPI
        [Fact]
        public void LogicalToDevice_UsesTheControlDpi_OrTheOverride() => Sta.Run(() =>
        {
            using var frm = new Form();
            var hasApi = typeof(System.Windows.Forms.Control).GetMethod("LogicalToDeviceUnits", new[] { typeof(int) }) != null;
            var expected = hasApi ? (int)typeof(System.Windows.Forms.Control).GetMethod("LogicalToDeviceUnits", new[] { typeof(int) }).Invoke(frm, new object[] { 10 }) : 10;
            Assert.Equal(expected, frm.LogicalToDevice(10));
            Assert.Equal(YANPaint.LogicalToDevice(frm, 17), frm.LogicalToDevice(17));
            Assert.Equal(0, frm.LogicalToDevice(0));
            YANPaint.DpiOverride = 144;
            try
            {
                Assert.Equal(15, frm.LogicalToDevice(10));
                Assert.Equal(3, frm.LogicalToDevice(2));
                Assert.Equal(26, frm.LogicalToDevice(17));
                Assert.Equal(-15, frm.LogicalToDevice(-10));
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
            // the override is per thread
            YANPaint.DpiOverride = 192;
            try
            {
                var other = 0;
                var t = new Thread(() => other = frm.LogicalToDevice(10));
                t.Start();
                t.Join();
                Assert.Equal(expected, other);
                Assert.Equal(20, frm.LogicalToDevice(10));
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // ScaleToDpi applies AutoScaleMode.Dpi at once: the form and its children are at the (overridden) DPI before any layout, and a
        // later layout scales nothing more
        [Fact]
        public void ScaleToDpi_ScalesADpiFormNow_AndOnlyOnce() => Sta.Run(() =>
        {
            Form Build()
            {
                var f = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
                f.SuspendLayout();
                f.AutoScaleDimensions = new SizeF(96F, 96F);
                f.AutoScaleMode = AutoScaleMode.Dpi;
                f.ClientSize = new Size(200, 100);
                f.Controls.Add(new Panel { Bounds = new Rectangle(10, 20, 40, 30) });
                f.ResumeLayout(false);
                return f;
            }
            using var at96 = Build();
            at96.ScaleToDpi();
            var machine = at96.CurrentAutoScaleDimensions.Width / 96f;
            Assert.Equal(at96.CurrentAutoScaleDimensions, at96.AutoScaleDimensions);
            Assert.Equal(new Size((int)Math.Round(200 * machine), (int)Math.Round(100 * machine)), at96.ClientSize);
            YANPaint.DpiOverride = 144;
            try
            {
                using var frm = Build();
                frm.ScaleToDpi();
                Assert.Equal(new Size(300, 150), frm.ClientSize);
                Assert.Equal(new Rectangle(15, 30, 60, 45), frm.Controls[0].Bounds);
                // the caller's bounds are not scaled again by the next layouts
                frm.Bounds = new Rectangle(50, 60, 400, 200);
                frm.PerformLayout();
                frm.CreateControl();
                Assert.Equal(new Rectangle(50, 60, 400, 200), frm.Bounds);
                Assert.Equal(new Rectangle(15, 30, 60, 45), frm.Controls[0].Bounds);
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // A small form at the top-left corner, not in the taskbar
        private static Form NewForm(double opacity = 1) => new()
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = Point.Empty,
            Size = new Size(200, 120),
            Opacity = opacity
        };

        // Processes window messages (so awaited fades run) until the condition holds; false after the timeout
        private static bool PumpUntil(Func<bool> condition, int timeoutMs = 5000)
        {
            var clock = Stopwatch.StartNew();
            while (!condition())
            {
                if (clock.ElapsedMilliseconds > timeoutMs)
                {
                    return false;
                }
                Application.DoEvents();
                Thread.Sleep(5);
            }
            return true;
        }

        // Processes window messages for a while
        private static void PumpFor(int ms) => PumpUntil(() => false, ms);

        // ShowDialog with a watchdog: a dialog that never closes fails the test instead of hanging it
        private static DialogResult ShowDialogGuarded(Form dlg)
        {
            var timedOut = false;
            using var watchdog = new System.Windows.Forms.Timer { Interval = 10000 };
            watchdog.Tick += (_, _) =>
            {
                watchdog.Stop();
                timedOut = true;
                dlg.Hide();
            };
            watchdog.Start();
            var result = dlg.ShowDialog();
            watchdog.Stop();
            Assert.False(timedOut, "the dialog did not close");
            return result;
        }

        // Runs the close action once the dialog is shown at full opacity (after its fade-in)
        private static IDisposable CloseWhenOpaque(Form dlg, Action close)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 10 };
            timer.Tick += (_, _) =>
            {
                if (dlg.Visible && dlg.Opacity >= 1 - 1e-6)
                {
                    timer.Stop();
                    close();
                }
            };
            timer.Start();
            return timer;
        }

        // Makes awaits resume on the test thread. .NET Framework gives every UI thread its own WindowsFormsSynchronizationContext
        // marshaling control; mono keeps one per process (made by the first UI thread, long gone for later tests), so a continuation
        // would never run: for the test, point mono's shared control at a control of this thread (a no-op on .NET Framework)
        private sealed class UiContext : IDisposable
        {
            private static readonly FieldInfo _monoInvokeControl = typeof(WindowsFormsSynchronizationContext).GetField("invoke_control", BindingFlags.NonPublic | BindingFlags.Static);
            private readonly System.Windows.Forms.Control _target = new();
            private readonly object _previous;

            private UiContext()
            {
                _ = _target.Handle;
                if (SynchronizationContext.Current is not WindowsFormsSynchronizationContext)
                {
                    SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                }
                if (_monoInvokeControl != null)
                {
                    _previous = _monoInvokeControl.GetValue(null);
                    _monoInvokeControl.SetValue(null, _target);
                }
            }

            public static UiContext Install() => new();

            public void Dispose()
            {
                if (_monoInvokeControl != null && ReferenceEquals(_monoInvokeControl.GetValue(null), _target))
                {
                    _monoInvokeControl.SetValue(null, _previous);
                }
                _target.Dispose();
            }
        }

        // Handlers on the events EnableFade uses (mono's Form subscribes to its own VisibleChanged, so compare with a baseline)
        private static int[] HandlerCounts(Form frm) => new[]
        {
            Handlers.Count(frm, nameof(Form.VisibleChanged)),
            Handlers.Count(frm, nameof(Form.FormClosing)),
            Handlers.Count(frm, nameof(Form.FormClosed))
        };
    }
}
