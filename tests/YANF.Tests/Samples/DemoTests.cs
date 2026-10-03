using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using YANF.Control;
using YANF.Demo;
using YANF.Screen;
using YANF.Script;
using static System.Windows.Forms.DialogResult;
using static YANF.Tests.Samples.SampleKit;
using static YANF.Tests.Services.LoaderKit;

namespace YANF.Tests.Samples
{
    using Control = System.Windows.Forms.Control;

    // Fixes in the sample application (samples/YANF.Demo), kept from the 1.0.3 smoke tests, and its use of the 1.1 and 2.0 API
    public class DemoTests
    {
        // 1.0.2 counted whole years only, so the age was one too high before the birthday
        [Fact]
        public void Demo1_Age_CountsTheBirthday() => Sta.Run(() =>
        {
            using var frm = new Demo1();
            var dp = Priv.Field<DateTimePicker>(frm, "dpNgS");
            var age = Priv.Field<Label>(frm, "lblT");
            var today = DateTime.Today;
            // birthday tomorrow
            dp.Value = today.AddYears(-30).AddDays(1);
            Assert.Equal("29 tuổi", age.Text);
            // birthday today
            dp.Value = today.AddYears(-30);
            Assert.Equal("30 tuổi", age.Text);
            // birthday yesterday
            dp.Value = today.AddYears(-30).AddDays(-1);
            Assert.Equal("30 tuổi", age.Text);
            // the time of day is ignored: not one year old yet
            dp.Value = today.AddYears(-1).AddDays(1).AddHours(13);
            Assert.True(string.IsNullOrEmpty(age.Text), $"under one year: '{age.Text}'");
            dp.Value = today.AddYears(-1).AddHours(23);
            Assert.Equal("1 tuổi", age.Text);
            // a future date
            dp.Value = today.AddDays(10);
            Assert.True(string.IsNullOrEmpty(age.Text), $"future date: '{age.Text}'");
            // a 29 February birthday counts from 1 March in non-leap years
            dp.Value = new DateTime(2000, 2, 29);
            var reached = today.Month > 2 || (today.Month == 2 && today.Day == 29);
            Assert.Equal($"{today.Year - 2000 - (reached ? 0 : 1)} tuổi", age.Text);
        });

        // 1.0.2 loaded a new Bitmap from the resources on every hover and never disposed them
        [Fact]
        public void Demo1_ExitImages_CachedAndDisposedWithTheForm() => Sta.Run(() =>
        {
            var frm = new Demo1();
            var btn = Priv.Field<Button>(frm, "btnExit");
            var normal = Priv.Field<Bitmap>(frm, "_pXI");
            var hover = Priv.Field<Bitmap>(frm, "_pXO");
            Assert.NotNull(normal);
            Assert.NotNull(hover);
            Assert.NotSame(normal, hover);
            Assert.False(Gdi.IsDisposed(normal) || Gdi.IsDisposed(hover), "cached images disposed too early");
            for (var i = 0; i < 3; i++)
            {
                Priv.Call(frm, "BtnExit_MouseEnter", btn, EventArgs.Empty);
                Assert.Same(hover, btn.BackgroundImage);
                Priv.Call(frm, "BtnExit_MouseLeave", btn, EventArgs.Empty);
                Assert.Same(normal, btn.BackgroundImage);
            }
            frm.Dispose();
            Assert.True(Gdi.IsDisposed(normal) && Gdi.IsDisposed(hover), "images not disposed with the form");
        });

        // 1.0.2 put the hover image back on MouseLeave of the Back button
        [Fact]
        public void Demo2_Leave_RestoresTheNormalImage() => Sta.Run(() =>
        {
            var frm = new Demo2();
            var back = Priv.Field<Button>(frm, "btnBack");
            var quit = Priv.Field<Button>(frm, "btnQuit");
            var backNormal = Priv.Field<Bitmap>(frm, "_pBI");
            var backHover = Priv.Field<Bitmap>(frm, "_pBO");
            var quitNormal = Priv.Field<Bitmap>(frm, "_pQI");
            var quitHover = Priv.Field<Bitmap>(frm, "_pQO");
            var all = new[] { backNormal, backHover, quitNormal, quitHover };
            Assert.Equal(4, all.Distinct().Count());
            Assert.DoesNotContain(all, img => Gdi.IsDisposed(img));
            // the designer-loaded normal images are the cached ones (no extra copies)
            Assert.Same(backNormal, back.BackgroundImage);
            Assert.Same(quitNormal, quit.BackgroundImage);
            for (var i = 0; i < 3; i++)
            {
                Priv.Call(frm, "BtnB_MouseEnter", back, EventArgs.Empty);
                Assert.Same(backHover, back.BackgroundImage);
                Priv.Call(frm, "BtnB_MouseLeave", back, EventArgs.Empty);
                Assert.Same(backNormal, back.BackgroundImage);
                Priv.Call(frm, "BtnQ_MouseEnter", quit, EventArgs.Empty);
                Assert.Same(quitHover, quit.BackgroundImage);
                Priv.Call(frm, "BtnQ_MouseLeave", quit, EventArgs.Empty);
                Assert.Same(quitNormal, quit.BackgroundImage);
            }
            frm.Dispose();
            Assert.All(all, img => Assert.True(Gdi.IsDisposed(img), "images not disposed with the form"));
        });

        // 1.1: EnableFade instead of the blocking FadeIn/FadeOut calls in Shown and FormClosing. 1.0 designed the forms at opacity 0
        // for FadeIn; they are designed opaque now (EnableFade makes them transparent until shown), so they also show without
        // Windows animations. LoaderKit.Run turns the cross-thread check on: 2.0.0 ran the fade-out on a thread-pool thread once
        // Application.DoEvents had left WinForms' thread-pool context on the thread, so it threw there and the form never closed
        [Theory]
        [InlineData(typeof(Demo1))]
        [InlineData(typeof(Demo2))]
        public void Forms_FadeInWhenShown_AndOutWhenClosed(Type type) => Run(() =>
        {
            try
            {
                YANDisplay.UIEffectsOverride = true;
                using var frm = (Form)Activator.CreateInstance(type);
                // transparent until shown, then faded in
                Assert.Equal(0, frm.Opacity, 3);
                frm.Show();
                Assert.True(PumpUntil(() => frm.Opacity >= 1 - 1e-6), "did not fade in: " + frm.Opacity);
                // the close waits for the fade-out
                var closedOpacity = -1d;
                frm.FormClosed += (_, _) => closedOpacity = frm.Opacity;
                frm.Close();
                Assert.False(frm.IsDisposed, "closed at once, without the fade-out");
                Assert.True(PumpUntil(() => frm.IsDisposed), "never closed");
                Assert.Equal(0, closedOpacity, 3);
                // Windows animations off: shown at full opacity and closed at once
                YANDisplay.UIEffectsOverride = false;
                using var plain = (Form)Activator.CreateInstance(type);
                plain.Show();
                PumpFor(50);
                Assert.Equal(1, plain.Opacity, 3);
                plain.Close();
                Assert.True(plain.IsDisposed, "not closed at once");
            }
            finally
            {
                YANDisplay.UIEffectsOverride = null;
            }
        });

        // 1.1: EnableDrag instead of the three 1.0 MoveFrm handlers, on the same surfaces as in 1.0: panels and labels. The inner
        // label of a YANDdl is left alone
        [Fact]
        public void Demo1_DragsByItsPassiveSurfaces() => Sta.Run(() =>
        {
            using var frm = new Demo1();
            var all = Descendants(frm).ToList();
            var surfaces = all.Where(c => c is Panel or Label && c.Parent is not YANDdl).ToList();
            Assert.NotEmpty(surfaces);
            foreach (var c in all)
            {
                var expected = surfaces.Contains(c) ? 1 : 0;
                Assert.True(expected == YanEventHandlers(c, nameof(Control.MouseDown)), $"{c.Name} ({c.GetType().Name}) in {c.Parent.Name}: {YanEventHandlers(c, nameof(Control.MouseDown))} drag handler(s), expected {expected}");
                Assert.Equal(0, YanEventHandlers(c, nameof(Control.MouseMove)) + YanEventHandlers(c, nameof(Control.MouseUp)));
            }
            Assert.Contains(all, c => c.Parent is YANDdl && c is Label);
        });

        // 2.0: Demo2 is a YANForm (borderless, rounded corners and a shadow, not resizable: its layout has a fixed size). The surfaces
        // that moved it in 1.x (panels, pictures and labels, YANGradPnl and YANCirPic included) are its caption controls instead of
        // EnableDrag handles
        [Fact]
        public void Demo2_IsAYANForm_MovedByItsPassiveSurfaces() => Sta.Run(() =>
        {
            using var frm = new Demo2();
            Assert.IsAssignableFrom<YANForm>(frm);
            Assert.Equal(FormBorderStyle.None, frm.FormBorderStyle);
            Assert.False(frm.Resizable, "resizable");
            Assert.True(frm.RoundedCorners, "square corners");
            Assert.True(frm.DropShadow, "no shadow");
            Assert.False(frm.MaximizeBox, "a double-click on a caption control would maximize it");
            var all = Descendants(frm).ToList();
            var surfaces = all.Where(c => c is Panel or Label or PictureBox).ToList();
            Assert.Contains(surfaces, c => c is YANGradPnl);
            Assert.Contains(surfaces, c => c is YANCirPic);
            foreach (var c in all)
            {
                Assert.True(surfaces.Contains(c) == frm.IsCaptionControl(c), $"{c.Name} ({c.GetType().Name}) in {c.Parent.Name}: caption control {frm.IsCaptionControl(c)}");
                Assert.Equal(0, YanEventHandlers(c, nameof(Control.MouseDown)) + YanEventHandlers(c, nameof(Control.MouseMove)) + YanEventHandlers(c, nameof(Control.MouseUp)));
            }
            // the buttons stay buttons
            Assert.False(frm.IsCaptionControl(Priv.Field<Button>(frm, "btnBack")), "Back is a caption control");
        });

        // 2.0: the sample is per-monitor DPI aware (app.manifest and App.config on .NET Framework, ApplicationHighDpiMode on .NET), so
        // its forms are designed at 96 dpi and scaled to the DPI of their monitor (1.x: AutoScaleMode.None, the same pixels at any DPI)
        [Theory]
        [InlineData(typeof(MainFrm))]
        [InlineData(typeof(Demo1))]
        [InlineData(typeof(Demo2))]
        public void Forms_ScaleWithTheDpi(Type type) => Sta.Run(() =>
        {
            using var frm = (Form)Activator.CreateInstance(type);
            Assert.Equal(AutoScaleMode.Dpi, frm.AutoScaleMode);
        });

        // 1.1: the Save button is focusable and the AcceptButton, so ENTER in a field saves (the 1.0 buttons never took the focus)
        [Fact]
        public void Demo1_Enter_PressesTheFocusableSaveButton() => Sta.Run(ui =>
        {
            try
            {
                YANDisplay.UIEffectsOverride = false;
                using var frm = new Demo1();
                var save = Priv.Field<YANBtn>(frm, "btnSave");
                Assert.Same(save, frm.AcceptButton);
                Assert.True(save.Focusable, "not focusable");
                Assert.True(save.TabStop, "TAB does not reach it");
                frm.Show();
                ui.Pump(3);
                Priv.Field<YANTxt>(frm, "txtHT").Focus();
                ui.Pump(3);
                string caption = null;
                using (var boxes = new BoxWatcher(box =>
                {
                    caption = Part(box, "lblCaption");
                    Press(box, OK);
                }))
                {
                    Assert.True((bool)Priv.Call(frm, "ProcessDialogKey", Keys.Enter), "ENTER was not handled");
                    boxes.Check(1);
                    Assert.Equal("完了！", boxes.Texts[0]);
                }
                Assert.Equal("情報", caption);
            }
            finally
            {
                YANDisplay.UIEffectsOverride = null;
            }
        });

        // 1.1: the Exit box is built from YANMessageBoxOptions: English, owned by the form, and Cancel is its default button
        [Fact]
        public void Demo1_Exit_AsksWithAnEnglishBox_WhereEnterKeepsTheForm() => Sta.Run(ui =>
        {
            try
            {
                YANDisplay.UIEffectsOverride = false;
                using var frm = new Demo1();
                var exit = Priv.Field<YANBtn>(frm, "btnExit");
                frm.Show();
                ui.Pump(3);
                // ENTER presses the default button: Cancel, the form stays
                using (var boxes = new BoxWatcher(box =>
                {
                    Assert.Equal("WARNING", Part(box, "lblCaption"));
                    Assert.Same(frm, box.Owner);
                    Assert.Equal(new[] { "OK", "Cancel" }, Buttons(box).OrderBy(b => b.DialogResult).Select(b => b.Text));
                    var dflt = Assert.IsAssignableFrom<Button>(box.AcceptButton);
                    Assert.Equal(Cancel, dflt.DialogResult);
                    dflt.PerformClick();
                }))
                {
                    Priv.Call(frm, "BtnExit_Click", exit, EventArgs.Empty);
                    boxes.Check(1);
                    Assert.Equal("If you close this window, all data will be lost!", boxes.Texts[0]);
                }
                Assert.False(frm.IsDisposed, "closed although Cancel was pressed");
                // OK closes it
                using (var boxes = new BoxWatcher(box => Press(box, OK)))
                {
                    Priv.Call(frm, "BtnExit_Click", exit, EventArgs.Empty);
                    boxes.Check(1);
                }
                Assert.True(frm.IsDisposed, "not closed after OK");
            }
            finally
            {
                YANDisplay.UIEffectsOverride = null;
            }
        });
    }
}
