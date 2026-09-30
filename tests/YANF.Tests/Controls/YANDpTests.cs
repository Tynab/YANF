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

        // 2.0: where the skin is not opaque, the parent's own pixels show through it (1.x showed the window color); an opaque skin
        // does not paint the parent at all (its Paint handlers do not run for the date picker)
        [Fact]
        public void Skin_ShowsTheParent_OnlyWhereItIsNotOpaque() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(200, 35), SkinColor = Color.Blue, Value = new DateTime(2026, 9, 29) };
            var pnl = AllControlsTests.OnParent(ui, new PaintingTests.LimePanel(), d, new Point(10, 10));
            var parentPaints = 0;
            pnl.Paint += (s, e) => parentPaints++;
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(5, 17), "opaque skin");
                Assert.Equal(0, parentPaints);
            }
            d.SkinColor = Color.Transparent;
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(5, 17), "transparent skin");
                Assert.True(parentPaints > 0, "the parent was not painted behind a transparent skin");
            }
            d.SkinColor = Color.FromArgb(128, Color.Blue);
            using (var bmp = ui.Render(d))
            {
                var color = bmp.GetPixel(5, 17);
                Assert.True(color.G is > 90 and < 165 && color.B is > 90 and < 165 && color.R < 40, "half-transparent blue over lime, got " + color);
            }
        });

        // A skin that is not opaque makes the date picker follow its parent: it repaints when the parent is invalidated behind it
        [Fact]
        public void TransparentSkin_FollowsTheParent() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(200, 35) };
            var pnl = new PaintingTests.LimePanel();
            ui.Show(pnl);
            pnl.Controls.Add(d);
            d.Location = new Point(10, 10);
            ui.Pump();
            var invalidated = 0;
            d.Invalidated += (s, e) => invalidated++;
            d.SkinColor = Color.Transparent;
            invalidated = 0;
            pnl.Invalidate(new Rectangle(0, 0, 20, 20));
            Assert.True(invalidated > 0, "not repainted when the parent was invalidated behind it");
        });

        // The border is a band of BorderSize 96-dpi pixels along the edges; at 192 dpi it is twice as wide
        [Fact]
        public void Border_BandScalesWithTheDpi() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(200, 35), SkinColor = Color.Blue, BorderColor = Color.Red, BorderSize = 3, Value = new DateTime(2026, 9, 29) };
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(1, 17), "border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(4, 17), "skin");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(100, 1), "top border");
            }
            YANPaint.DpiOverride = 192;
            try
            {
                using var bmp = ui.Render(d);
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(4, 17), "192 dpi border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(7, 17), "192 dpi skin");
                Assert.Equal(3, d.BorderSize);
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // The icon button area and the calendar icon are 96-dpi sizes scaled to the DPI: the icon is scaled once per DPI (and per
        // skin brightness), and the image it replaces is released
        [Fact]
        public void Icon_ScalesWithTheDpi_AndIsReleased() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(300, 35), Format = DateTimePickerFormat.Short, Value = new DateTime(2026, 9, 29) };
            ui.Draw(d);
            var icon = Priv.Field<Image>(d, "_calIc");
            Assert.Equal(new Size(16, 16), icon.Size);
            Assert.Equal(34, IconWidth(d));
            ui.Draw(d);
            Assert.Same(icon, Priv.Field<Image>(d, "_calIc"));
            YANPaint.DpiOverride = 192;
            try
            {
                ui.Draw(d);
                var scaled = Priv.Field<Image>(d, "_calIc");
                Assert.Equal(new Size(32, 32), scaled.Size);
                Assert.True(Gdi.IsDisposed(icon), "the icon built for 96 dpi was not released");
                Assert.Equal(68, IconWidth(d));
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
            // a light skin takes the black icon
            d.SkinColor = Color.White;
            ui.Draw(d);
            var black = Priv.Field<Image>(d, "_calIc");
            Assert.Equal(new Size(16, 16), black.Size);
            d.Dispose();
            Assert.True(Gdi.IsDisposed(black), "the icon was not released with the control");
        });

        // The text is drawn with TextRenderer, which also measures it for the icon button area, indented as in 1.x
        [Fact]
        public void Text_DrawnInTheTextColor() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(250, 35), SkinColor = Color.Blue, TextColor = Color.Lime, Value = new DateTime(2026, 9, 29) };
            using var bmp = ui.Render(d);
            Assert.True(Gdi.Count(bmp, Gdi.IsLime) > 10, "no text drawn");
            // the three-space indent: nothing of the text in the first pixels
            for (var x = 0; x < 6; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    Assert.False(Gdi.IsLime(bmp.GetPixel(x, y)), $"text drawn at ({x}, {y}), inside the indent");
                }
            }
        });

        // High contrast: the skin, the text and the border take the system colors (the configured ones are not changed)
        [Fact]
        public void HighContrast_PaintsWithSystemColors() => Sta.Run(ui =>
        {
            var d = new YANDp { Size = new Size(200, 35), SkinColor = Color.Blue, BorderColor = Color.Red, BorderSize = 3, Value = new DateTime(2026, 9, 29) };
            YANPaint.HighContrastOverride = true;
            try
            {
                using var bmp = ui.Render(d);
                PaintingTests.AssertColor(SystemColors.Window, bmp.GetPixel(5, 17), "skin");
                PaintingTests.AssertColor(SystemColors.WindowFrame, bmp.GetPixel(1, 17), "border");
                Assert.Equal(Color.Blue, d.SkinColor);
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
        });

        private static RectangleF IconArea(YANDp d) => Priv.Field<RectangleF>(d, "_icBtnArea");

        private static int IconWidth(YANDp d) => (int)Priv.Call(d, "GetWIcBtn");
    }
}
