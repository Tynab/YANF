using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Control;
using static YANF.Script.YANConstant;

namespace YANF.Tests.Controls
{
    public class YANPrgTests
    {
        [Fact]
        public void ValueAtMinMaxAndEmptyRange_Draw() => Sta.Run(ui =>
        {
            var ranges = new (int Min, int Max, int Value)[] { (0, 100, 0), (0, 100, 50), (0, 100, 100), (5, 5, 5), (0, 0, 0), (int.MaxValue - 1, int.MaxValue, int.MaxValue) };
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var (min, max, value) in ranges)
                {
                    foreach (PrgTextPosition align in Enum.GetValues(typeof(PrgTextPosition)))
                    {
                        ui.Case($"YANPrg {size.Width}x{size.Height} {min}..{max}={value} {align}", () =>
                        {
                            var p = Sweep.Sized(new YANPrg { TextAlign = align, ShowMaximum = true, SymbolAfter = "%" }, size);
                            p.Maximum = max;
                            p.Minimum = min;
                            p.Value = value;
                            Assert.Equal((min, max, value), (p.Minimum, p.Maximum, p.Value));
                            ui.DrawAndDispose(p);
                        });
                    }
                }
            }
        });

        // 1.0.2 stopped painting for good once Value reached Maximum
        [Fact]
        public void Repaints_AfterMaximum() => Sta.Run(ui =>
        {
            var p = new YANPrg { Size = new Size(200, 40), TextAlign = PrgTextPosition.Sliding };
            p.Value = 100;
            ui.Draw(p);
            p.Value = 10;
            ui.Draw(p);
            p.Value = 0;
            ui.Draw(p);
            // negative channel heights are ignored, as before
            p.ChannelHeight = -1;
            Assert.Equal(6, p.ChannelHeight);
            p.SliderHeight = 30;
            p.ChannelHeight = 0;
            ui.Draw(p);
        });

        [Fact]
        public void Repaints_EveryTime_Pixels() => Sta.Run(ui =>
        {
            var p = new YANPrg { Size = new Size(200, 40), TextAlign = PrgTextPosition.None, ChannelColor = Color.Red, SliderColor = Color.Blue };
            p.Value = 100;
            using (var bmp = ui.Render(p))
            {
                Assert.True(Gdi.Same(bmp.GetPixel(190, 37), Color.Blue), "full slider expected at Value == Maximum, got " + bmp.GetPixel(190, 37));
            }
            p.Value = 10;
            using (var bmp = ui.Render(p))
            {
                Assert.True(Gdi.Same(bmp.GetPixel(10, 37), Color.Blue), "slider expected at x=10, got " + bmp.GetPixel(10, 37));
                Assert.True(Gdi.Same(bmp.GetPixel(190, 37), Color.Red), "channel expected at x=190 after the value went down, got " + bmp.GetPixel(190, 37));
            }
            // an empty range shows no progress
            p.Maximum = 0;
            using (var bmp = ui.Render(p))
            {
                Assert.True(Gdi.Same(bmp.GetPixel(100, 37), Color.Red), "an empty range must show only the channel, got " + bmp.GetPixel(100, 37));
            }
        });

        // In the sliding style the band of the value over the progress shows the parent again: its own pixels (lime), where 1.x
        // filled it with Parent.BackColor (white). The bars are taller than the band (the text height + 2) with any default font:
        // Segoe UI 9pt on .NET, whose band nearly fills a 20 px bar, Microsoft Sans Serif 8.25pt on .NET Framework
        [Fact]
        public void Sliding_BandOverTheProgress_ShowsTheParent() => Sta.Run(ui =>
        {
            var p = new YANPrg { Size = new Size(200, 40), ChannelHeight = 40, SliderHeight = 40, ChannelColor = Color.Red, SliderColor = Color.Blue, TextAlign = PrgTextPosition.Sliding };
            p.Value = 50;
            var pnl = new PaintingTests.LimePanel();
            ui.Host.Controls.Add(pnl);
            p.Location = new Point(10, 10);
            pnl.Controls.Add(p);
            using var bmp = ui.Render(p);
            // in the client area: without visual styles (the test process) Windows puts a 1-pixel frame around a progress bar
            var o = Ui.ClientOrigin(p);
            PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(o.X + 2, o.Y + 2), "band over the progress, left of the value");
            PaintingTests.AssertColor(Color.Red, bmp.GetPixel(o.X + 150, o.Y + 2), "channel right of the progress");
            var band = TextRenderer.MeasureText("50%", p.Font).Height + 2;
            Assert.True(band < p.ClientSize.Height - 1, $"the {band} px band leaves no slider row below it");
            PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(o.X + 2, o.Y + p.ClientSize.Height - 1), "slider below the band");
        });

        // ChannelHeight and SliderHeight are 96-dpi pixels: they double at 192 dpi (a DPI-unaware application stays at 96 dpi). The
        // bars lie at the bottom of the client area, whole: without visual styles (the test process) Windows puts a 1-pixel frame
        // around a progress bar
        [Fact]
        public void Dpi_ScalesTheBars() => Sta.Run(ui =>
        {
            var p = new YANPrg { Size = new Size(200, 40), TextAlign = PrgTextPosition.None, ChannelColor = Color.Red, SliderColor = Color.Blue };
            // the row below the client area in the bitmap
            int Bottom() => Ui.ClientOrigin(p).Y + p.ClientSize.Height;
            using (var bmp = ui.Render(p))
            {
                Assert.True(Gdi.Same(bmp.GetPixel(100, Bottom() - 6), Color.Red), "96 dpi: 6 px channel");
                Assert.False(Gdi.Same(bmp.GetPixel(100, Bottom() - 7), Color.Red), "96 dpi: 6 px channel");
            }
            YANPaint.DpiOverride = 192;
            try
            {
                using var bmp = ui.Render(p);
                Assert.True(Gdi.Same(bmp.GetPixel(100, Bottom() - 12), Color.Red), "192 dpi: 12 px channel");
                Assert.False(Gdi.Same(bmp.GetPixel(100, Bottom() - 13), Color.Red), "192 dpi: 12 px channel");
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // The value is drawn with TextRenderer, which measured it: the text stays inside its box at the right
        [Fact]
        public void Value_DrawnInsideItsMeasuredBox() => Sta.Run(ui =>
        {
            var p = new YANPrg
            {
                Size = new Size(300, 60),
                Font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold),
                ForeColor = Color.Lime,
                ValueBackColor = Color.White,
                ShowMaximum = true,
                SymbolAfter = "&"
            };
            using var bmp = ui.Render(p);
            var width = TextRenderer.MeasureText("0&/100&", p.Font, Size.Empty, TextFormatFlags.NoPrefix).Width;
            // at the right of the client area (inside the 1-pixel frame of a progress bar without visual styles)
            var inside = Gdi.Count(bmp, Gdi.IsLime, Ui.ClientOrigin(p).X + p.ClientSize.Width - width);
            Assert.True(inside > 0, "no value drawn");
            Assert.Equal(inside, Gdi.Count(bmp, Gdi.IsLime));
        });

        // High contrast mode paints system colors
        [Fact]
        public void HighContrast_PaintsSystemColors() => Sta.Run(ui =>
        {
            YANPaint.HighContrastOverride = true;
            try
            {
                var p = new YANPrg { Size = new Size(200, 40), TextAlign = PrgTextPosition.Left };
                p.Value = 50;
                using var bmp = ui.Render(p);
                PaintingTests.AssertColor(SystemColors.Highlight, bmp.GetPixel(80, 37), "slider");
                PaintingTests.AssertColor(SystemColors.ControlText, bmp.GetPixel(150, 37), "channel");
                PaintingTests.AssertColor(SystemColors.Highlight, bmp.GetPixel(1, 1), "value box");
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
        });
    }
}
