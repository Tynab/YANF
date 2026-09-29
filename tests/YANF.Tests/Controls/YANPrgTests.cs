using System;
using System.Drawing;
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
    }
}
