using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANGradPnlTests
    {
        [Fact]
        public void AllSizesAndAngles_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var angle in new[] { 0f, 45f, 90f, 360f })
                {
                    ui.Case($"YANGradPnl {size.Width}x{size.Height} a={angle}", () => ui.DrawAndDispose(Sweep.Sized(new YANGradPnl { Angle = angle }, size)));
                }
            }
        });

        // 1.0.2 painted in OnPaint without ResizeRedraw or double buffering: stale gradients and flicker on resize
        [Fact]
        public void PaintStyles_AndResize_Draw() => Sta.Run(ui =>
        {
            var p = new YANGradPnl { TopColor = Color.Red, BottomColor = Color.Blue, Angle = 90 };
            p.Controls.Add(new Label { Text = "child", BackColor = Color.Transparent, Location = new Point(2, 2) });
            Assert.True(GetStyle(p, ControlStyles.ResizeRedraw), "ResizeRedraw");
            Assert.True(GetStyle(p, ControlStyles.OptimizedDoubleBuffer), "OptimizedDoubleBuffer");
            Assert.True(GetStyle(p, ControlStyles.AllPaintingInWmPaint), "AllPaintingInWmPaint");
            foreach (var size in new[] { new Size(200, 40), new Size(0, 0), new Size(1, 1), new Size(0, 50), new Size(50, 0), new Size(640, 480), new Size(3, 10) })
            {
                ui.Case($"YANGradPnl resized to {size.Width}x{size.Height}", () =>
                {
                    p.Size = size;
                    ui.Draw(p);
                });
            }
        });

        // The gradient is the background (OnPaintBackground), so it is there at every size
        [Fact]
        public void Gradient_PaintedAsBackground() => Sta.Run(ui =>
        {
            var p = new YANGradPnl { Size = new Size(100, 100), TopColor = Color.Red, BottomColor = Color.Red, Angle = 90 };
            using (var bmp = ui.Render(p))
            {
                Assert.True(Gdi.Same(bmp.GetPixel(50, 50), Color.Red), "gradient missing, got " + bmp.GetPixel(50, 50));
            }
            p.Size = new Size(300, 20);
            p.BottomColor = Color.Blue;
            // Sampled inside the gradient: GDI+ can paint the first row of a LinearGradientBrush fill in the end color when the
            // brush rectangle equals the filled one. x = 299 still proves that the gradient covers the new width.
            using (var bmp = ui.Render(p))
            {
                Assert.True(bmp.GetPixel(299, 2).R > 200, "gradient top after resize, got " + bmp.GetPixel(299, 2));
                Assert.True(bmp.GetPixel(150, 17).B > 200, "gradient bottom after resize, got " + bmp.GetPixel(150, 17));
            }
        });

        // The gradient does not wrap: its first row has the top color and its last row the bottom color (GDI+ can paint the first row
        // of a LinearGradientBrush fill in the end color when the brush rectangle is the filled one)
        [Fact]
        public void Gradient_DoesNotWrapAtTheEdges() => Sta.Run(ui =>
        {
            var p = new YANGradPnl { Size = new Size(100, 20), TopColor = Color.Red, BottomColor = Color.Blue, Angle = 90 };
            using var bmp = ui.Render(p);
            foreach (var x in new[] { 0, 50, 99 })
            {
                Assert.True(bmp.GetPixel(x, 0).R > bmp.GetPixel(x, 0).B, $"first row at x = {x}: {bmp.GetPixel(x, 0)}");
                Assert.True(bmp.GetPixel(x, 19).B > bmp.GetPixel(x, 19).R, $"last row at x = {x}: {bmp.GetPixel(x, 19)}");
            }
        });

        // A child that paints its parent asks for the part behind it: only that part is filled, with the gradient of the whole panel
        [Fact]
        public void Background_PaintsOnlyTheAreaToPaint() => Sta.Run(ui =>
        {
            using var p = new YANGradPnl { Size = new Size(100, 100), TopColor = Color.Red, BottomColor = Color.Blue, Angle = 90 };
            using var bmp = new Bitmap(100, 100);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Lime);
                using var e = new PaintEventArgs(g, new Rectangle(40, 40, 20, 20));
                Priv.Call(p, "OnPaintBackground", e);
            }
            Assert.True(Gdi.IsLime(bmp.GetPixel(10, 10)), "painted outside the area to paint");
            Assert.True(Gdi.IsLime(bmp.GetPixel(70, 50)), "painted outside the area to paint");
            var inside = bmp.GetPixel(50, 50);
            // halfway down the whole panel: as much red as blue
            Assert.True(Math.Abs(inside.R - inside.B) < 30 && inside.G < 40, "the gradient of the whole panel, got " + inside);
        });

        // An opaque gradient covers the BackColor (it is not painted); a gradient that is not opaque shows it, as in 1.x
        [Fact]
        public void BackColor_OnlyUnderAGradientThatIsNotOpaque() => Sta.Run(ui =>
        {
            var p = new YANGradPnl { Size = new Size(100, 100), BackColor = Color.Red, TopColor = Color.Blue, BottomColor = Color.Blue };
            using (var bmp = ui.Render(p))
            {
                Assert.Equal(0, Gdi.Count(bmp, Gdi.IsRed));
            }
            p.TopColor = Color.Transparent;
            p.BottomColor = Color.Transparent;
            using (var bmp = ui.Render(p))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(50, 50), "BackColor under a transparent gradient");
            }
        });

        // #15: the YANF controls that paint their parent behind their rounded corners show the gradient, not the panel's BackColor
        [Fact]
        public void Children_SeeTheGradient() => Sta.Run(ui =>
        {
            var p = new YANGradPnl { Size = new Size(300, 300), TopColor = Color.Red, BottomColor = Color.Blue, BackColor = Color.White, Angle = 90 };
            ui.Host.Controls.Add(p);
            var children = new System.Windows.Forms.Control[] { new YANBtn { Location = new Point(10, 10) }, new YANCirPic { Location = new Point(10, 70) }, new YANTg { Location = new Point(10, 190) } };
            foreach (var c in children)
            {
                p.Controls.Add(c);
                c.Visible = false;
            }
            using var bmpPanel = ui.Render(p);
            foreach (var c in children)
            {
                c.Visible = true;
                using var bmp = ui.Render(c);
                var behind = bmpPanel.GetPixel(c.Left + 1, c.Top + 1);
                Assert.False(Gdi.Same(behind, Color.White), "the gradient must differ from the panel's BackColor");
                PaintingTests.AssertColor(behind, bmp.GetPixel(1, 1), $"{c.GetType().Name}: the gradient behind the corner", 3);
            }
        });

        private static bool GetStyle(System.Windows.Forms.Control c, ControlStyles style) => (bool)Priv.Call(c, "GetStyle", style);
    }
}
