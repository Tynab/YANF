using System;
using System.ComponentModel;
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
            // 2.0: the parent is painted around the circle, no region
            Assert.Null(c.Region);
        });

        // 2.0 keeps the 1.x geometry at 96 dpi: the circle is inset by one pixel from the square (the 1.x region), the border lies
        // inside it after a ring of half its width (rounded up to whole pixels), and the ring, where 1.x painted Parent.BackColor over
        // the picture, shows the parent
        [Fact]
        public void Border_KeepsThe1xGeometry() => Sta.Run(ui =>
        {
            var c = OnLime(ui, new YANCirPic { BorderSize = 4 });
            YANPaint.HighContrastOverride = true;
            try
            {
                // one color for the border: the system frame color of high contrast mode
                using var bmp = ui.Render(c);
                var frame = SystemColors.WindowFrame;
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(50, 0), "outside the contour");
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(50, 2), "the ring where 1.x painted Parent.BackColor");
                PaintingTests.AssertColor(frame, bmp.GetPixel(50, 4), "border");
                PaintingTests.AssertColor(frame, bmp.GetPixel(50, 5), "border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(50, 9), "picture");
                PaintingTests.AssertColor(frame, bmp.GetPixel(4, 50), "border, left");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(50, 50), "centre");
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
            // the configured gradient border
            c.BorderTopColor = Color.Red;
            c.BorderBottomColor = Color.Red;
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(50, 4), "gradient border");
            }
            // the ring is half the border rounded up to whole pixels, and the pen is centred on the band after it, so that an odd
            // border covers whole pixels as the 1.x pens did (BorderSize 1: the ring on row 1, the border on row 2)
            foreach (var (border, ring) in new[] { (1, 1), (2, 1), (3, 2), (4, 2) })
            {
                c.BorderSize = border;
                var args = new object[3];
                Assert.True((bool)Priv.Call(c, "GetCircle", args));
                var (rectCircle, rectBorder, size) = ((RectangleF)args[0], (RectangleF)args[1], (int)args[2]);
                Assert.Equal(border, size);
                Assert.Equal(new RectangleF(1 + ring, 1 + ring, 98 - 2 * ring, 98 - 2 * ring), rectCircle);
                var inset = 1 + ring + border / 2f;
                Assert.Equal(new RectangleF(inset, inset, 100 - 2 * inset, 100 - 2 * inset), rectBorder);
            }
        });

        // An odd border is painted in whole pixels, as 1.x painted it: outside the contour (row 0) and in the ring (half the border
        // rounded up) the parent shows, then whole rows of border, with no row blended half and half. Tolerance: the circle bends a
        // little across the pixel at its top and left
        [Fact]
        public void Border_OddSize_StaysCrisp() => Sta.Run(ui =>
        {
            var c = OnLime(ui, new YANCirPic { BorderTopColor = Color.Red, BorderBottomColor = Color.Red });
            foreach (var (border, ring) in new[] { (1, 1), (3, 2) })
            {
                c.BorderSize = border;
                using var bmp = ui.Render(c);
                for (var i = 0; i < 8; i++)
                {
                    var expected = i < 1 + ring ? Color.Lime : i < 1 + ring + border ? Color.Red : Color.Blue;
                    PaintingTests.AssertColor(expected, bmp.GetPixel(50, i), $"BorderSize {border}, top row {i}", 8);
                    PaintingTests.AssertColor(expected, bmp.GetPixel(i, 50), $"BorderSize {border}, left column {i}", 8);
                }
            }
        });

        // A BorderStyle frame is drawn by Windows outside the client area, where the parent cannot be painted: the control is cut
        // to the circle with a region (the 1.x region, moved inside the frame), built with the size and the frame, never while
        // painting. Without a frame there is no region, and the application's own region is kept
        [Fact]
        public void BorderStyle_CutsTheFrameWithARegion() => Sta.Run(ui =>
        {
            var c = OnLime(ui, new YANCirPic());
            Assert.Null(c.Region);
            foreach (var style in new[] { BorderStyle.FixedSingle, BorderStyle.Fixed3D })
            {
                c.BorderStyle = style;
                ui.Draw(c);
                Assert.NotNull(c.Region);
                var frame = (c.Width - c.ClientSize.Width) / 2;
                var side = Math.Min(c.ClientSize.Width, c.ClientSize.Height);
                using (var bmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bmp))
                {
                    // the circle inset by one pixel from the square, in window coordinates
                    var bounds = c.Region.GetBounds(g);
                    Assert.InRange(bounds.X, frame + 1 - 1, frame + 1 + 1);
                    Assert.InRange(bounds.Y, frame + 1 - 1, frame + 1 + 1);
                    Assert.InRange(bounds.Width, side - 2 - 1, side - 2 + 1);
                }
                var changes = 0;
                c.RegionChanged += (s, e) => changes++;
                ui.Draw(c);
                ui.Draw(c);
                Assert.Equal(0, changes);
            }
            c.BorderStyle = BorderStyle.None;
            ui.Draw(c);
            Assert.Null(c.Region);
            using var own = new Region(new Rectangle(0, 0, 50, 20));
            c.Region = own;
            c.Size = new Size(80, 80);
            ui.Draw(c);
            Assert.Same(own, c.Region);
            c.Region = null;
        });

        // BorderSize is in 96-dpi pixels: at 192 dpi the border, the ring and the inset are twice as wide
        [Fact]
        public void Border_ScalesWithTheDpi() => Sta.Run(ui =>
        {
            var c = OnLime(ui, new YANCirPic { BorderSize = 2, BorderTopColor = Color.Red, BorderBottomColor = Color.Red });
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(50, 3), "96 dpi border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(50, 5), "96 dpi picture");
            }
            YANPaint.DpiOverride = 192;
            try
            {
                using var bmp = ui.Render(c);
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(50, 2), "192 dpi ring");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(50, 5), "192 dpi border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(50, 10), "192 dpi picture");
                Assert.Equal(2, c.BorderSize);
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // An opaque gradient hides the background: the BackColor is not painted (and a transparent BackColor does not paint the
        // parent twice). A gradient that is not opaque shows the BackColor inside the circle, as in 1.x
        [Fact]
        public void Background_OnlyUnderAGradientThatIsNotOpaque() => Sta.Run(ui =>
        {
            var c = OnLime(ui, new YANCirPic { BackColor = Color.Red });
            using (var bmp = ui.Render(c))
            {
                Assert.Equal(0, Gdi.Count(bmp, Gdi.IsRed));
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(50, 50), "opaque gradient");
            }
            c.TopColor = Color.Transparent;
            c.BottomColor = Color.Transparent;
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(50, 50), "BackColor under a transparent gradient");
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(1, 1), "corner");
            }
        });

        // The image drawn by the base class (SizeMode) is cut by the circle, anti-aliased, with the parent around it
        [Fact]
        public void Image_CutByTheCircle() => Sta.Run(ui =>
        {
            // at the size of the control: StretchImage draws it pixel for pixel, without blending its edges
            using var image = new Bitmap(100, 100);
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.Red);
            }
            var c = OnLime(ui, new YANCirPic { Image = image, BorderSize = 0 });
            using var bmp = ui.Render(c);
            PaintingTests.AssertColor(Color.Red, bmp.GetPixel(50, 50), "stretched image");
            PaintingTests.AssertColor(Color.Red, bmp.GetPixel(50, 3), "image near the edge");
            PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(3, 3), "corner");
            Assert.True(Gdi.Count(bmp, color => color.B < 40 && color.R is > 30 and < 225 && color.G is > 30 and < 225) > 0, "the edge of the image is not anti-aliased");
        });

        // 2.0 redeclares SizeMode with the constructor's default, StretchImage: the designer writes Normal (1.x lost it) and no longer
        // writes StretchImage; the property keeps the framework's category and localizability
        [Fact]
        public void SizeMode_DesignerDefaultIsStretchImage() => Sta.Run(() =>
        {
            using var c = new YANCirPic();
            var prop = TypeDescriptor.GetProperties(c)[nameof(YANCirPic.SizeMode)];
            Assert.Equal(typeof(YANCirPic), prop.ComponentType);
            Assert.Equal(PictureBoxSizeMode.StretchImage, ((DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]).Value);
            Assert.True(((LocalizableAttribute)prop.Attributes[typeof(LocalizableAttribute)]).IsLocalizable);
            Assert.Equal(PictureBoxSizeMode.StretchImage, c.SizeMode);
            Assert.False(prop.ShouldSerializeValue(c));
            c.SizeMode = PictureBoxSizeMode.Normal;
            Assert.Equal(PictureBoxSizeMode.Normal, ((PictureBox)c).SizeMode);
            Assert.True(prop.ShouldSerializeValue(c), "Normal must be written");
            prop.ResetValue(c);
            Assert.Equal(PictureBoxSizeMode.StretchImage, c.SizeMode);
            ((PictureBox)c).SizeMode = PictureBoxSizeMode.Zoom;
            Assert.Equal(PictureBoxSizeMode.Zoom, c.SizeMode);
        });

        // A 100 x 100 picture with a blue gradient, on a lime parent that is white underneath
        private static YANCirPic OnLime(Ui ui, YANCirPic c)
        {
            c.Size = new Size(100, 100);
            c.TopColor = Color.Blue;
            c.BottomColor = Color.Blue;
            AllControlsTests.OnParent(ui, new PaintingTests.LimePanel(), c, new Point(20, 20));
            return c;
        }
    }
}
