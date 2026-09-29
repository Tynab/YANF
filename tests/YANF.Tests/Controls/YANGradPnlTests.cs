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

        private static bool GetStyle(System.Windows.Forms.Control c, ControlStyles style) => (bool)Priv.Call(c, "GetStyle", style);
    }
}
