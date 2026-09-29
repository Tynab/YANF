using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANCirPicTests
    {
        [Fact]
        public void BigBorder_Draws() => Sta.Run(ui => ui.DrawAndDispose(new YANCirPic
        {
            Size = new Size(100, 100),
            BorderSize = 49
        }));

        [Fact]
        public void AllSizesAndBorders_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var border in new[] { 0, 1, 2, 25, 48, 49, 50, 51, 100, 1000, -3 })
                {
                    ui.Case($"YANCirPic {size.Width}x{size.Height} b={border}", () =>
                    {
                        var c = Sweep.Sized(new YANCirPic { BorderSize = border }, size);
                        Assert.Equal(Math.Max(0, border), c.BorderSize);
                        ui.DrawAndDispose(c);
                    });
                }
            }
        });

        [Theory]
        [InlineData(48)]
        [InlineData(49)]
        [InlineData(50)]
        public void BorderAroundHalfWidth_Draws(int border) => Sta.Run(ui => ui.DrawAndDispose(new YANCirPic
        {
            Size = new Size(100, 100),
            BorderSize = border
        }));

        [Fact]
        public void Resize_KeepsBorderSize() => Sta.Run(ui =>
        {
            var c = new YANCirPic { BorderSize = 30 };
            c.Size = new Size(20, 20);
            c.Size = new Size(100, 100);
            Assert.Equal(30, c.BorderSize);
            ui.DrawAndDispose(c);
        });

        [Fact]
        public void Undocked_StaysSquare() => Sta.Run(ui =>
        {
            var c = new YANCirPic();
            var resizes = 0;
            c.Resize += (s, e) => resizes++;
            c.Size = new Size(120, 40);
            Assert.Equal(new Size(120, 120), c.Size);
            Assert.True(resizes <= 3, "resize re-entered too often: " + resizes);
            // a maximum size that forbids the square must not loop or throw
            c.MaximumSize = new Size(200, 60);
            c.Size = new Size(150, 50);
            ui.DrawAndDispose(c);
        });

        // 1.0.2 forced Size = (Width, Width) in OnResize, which fought Dock and Anchor
        [Fact]
        public void DockFill_FollowsParent() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(300, 100) };
            ui.Host.Controls.Add(pnl);
            var c = new YANCirPic { BorderSize = 5, Dock = DockStyle.Fill };
            pnl.Controls.Add(c);
            Assert.Equal(new Size(300, 100), c.Size);
            ui.Draw(c);
            pnl.Size = new Size(50, 400);
            Assert.Equal(new Size(50, 400), c.Size);
            ui.Draw(c);
            var changes = 0;
            c.RegionChanged += (s, e) => changes++;
            ui.Draw(c);
            Assert.Equal(0, changes);
        });
    }
}
