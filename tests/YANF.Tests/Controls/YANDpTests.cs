using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANDpTests
    {
        [Fact]
        public void AllSizesAndBorders_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var border in new[] { 0, 1, 2, 20, 25, 49, 1000, -3 })
                {
                    ui.Case($"YANDp {size.Width}x{size.Height} b={border}", () =>
                    {
                        var d = Sweep.Sized(new YANDp { BorderSize = border }, size);
                        Assert.Equal(Math.Max(0, border), d.BorderSize);
                        ui.DrawAndDispose(d);
                    });
                }
            }
        });

        // The calendar icon's hit area was computed once; it must follow the control's width
        [Fact]
        public void IconArea_FollowsSize() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(250, 35) };
            ui.Draw(d);
            Assert.Equal(250f, IconArea(d).Right);
            d.Width = 400;
            Assert.Equal(400f, IconArea(d).Right);
            d.BorderSize = 30;
            d.Size = new Size(60, 35);
            Assert.Equal(30, d.BorderSize);
            ui.Draw(d);
        });

        // The icon width depends on the text width, which depends on the format. The handle is recreated on a Format change
        // and the Value/CustomFormat are applied after OnHandleCreated, so the area must be recomputed when the format changes
        [Fact]
        public void IconArea_FollowsFormat() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(150, 35), Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 9, 29) };
            ui.Draw(d);
            Assert.Equal((float)IconWidth(d), IconArea(d).Width);
            d.Format = DateTimePickerFormat.Long;
            var expected = IconWidth(d);
            var area = IconArea(d);
            Assert.Equal((float)expected, area.Width);
            Assert.Equal(150f, area.Right);
            ui.Draw(d);
        });

        // 1.0.2 drew on CreateGraphics() inside OnPaint, so DrawToBitmap/WM_PRINT got nothing
        [Fact]
        public void PaintsInto_PaintEventGraphics() => Sta.Run(ui =>
        {
            var d = new YANDp { SkinColor = Color.Red, Size = new Size(200, 35) };
            using var bmp = ui.Render(d);
            // left of the text (it starts after three spaces) and far from the icon on the right
            Assert.True(Gdi.Same(bmp.GetPixel(5, 17), Color.Red), "skin color not painted into the bitmap, got " + bmp.GetPixel(5, 17));
        });

        // 1.0 forced TabStop = false: TAB now reaches a new date picker (1.0 designer files keep their explicit TabStop = false)
        [Fact]
        public void Tab_ReachesTheDatePicker() => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var legacy = new YANDp { TabStop = false };
            var d = new YANDp();
            var frm = ui.Show(txt, legacy, d);
            Assert.True(d.TabStop);
            Assert.True(frm.SelectNextControl(txt, true, true, true, true));
            Assert.Same(d, frm.ActiveControl);
        });

        // Typed characters stay ignored: with UserPaint the native control never shows which day, month or year field they
        // would edit (the arrow keys and the drop-down calendar keep working)
        [Fact]
        public void TypedCharacters_StillIgnored() => Sta.Run(() =>
        {
            using var d = new YANDp();
            var e = new KeyPressEventArgs('5');
            Priv.Call(d, "OnKeyPress", e);
            Assert.True(e.Handled);
        });

        [Fact]
        public void FocusCue_AllSizesAndBorders_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var border in new[] { 0, 1, 20, 1000 })
                {
                    ui.Case($"focused YANDp {size.Width}x{size.Height} b={border}", () => ui.DrawAndDispose(Sweep.Sized(new AllControlsTests.FocusedDp { BorderSize = border }, size)));
                }
            }
        });

        private static RectangleF IconArea(YANDp d) => Priv.Field<RectangleF>(d, "_icBtnArea");

        private static int IconWidth(YANDp d) => (int)Priv.Call(d, "GetWIcBtn");
    }
}
