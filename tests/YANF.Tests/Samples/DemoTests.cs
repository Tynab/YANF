using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using YANF.Demo;

namespace YANF.Tests.Samples
{
    // Fixes in the sample application (samples/YANF.Demo), kept from the 1.0.3 smoke tests
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
    }
}
