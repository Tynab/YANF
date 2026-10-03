using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Xunit;
using YANF.Script;

namespace YANF.Tests.Script
{
    // The internal geometry helper behind every rounded control and screen (InternalsVisibleTo)
    public class YANShapeTests
    {
        [Theory]
        [InlineData(100f, 40f, 10f, 10f)]
        [InlineData(100f, 40f, 30f, 20f)]
        [InlineData(100f, 40f, -5f, 0f)]
        [InlineData(0f, 40f, 10f, 0f)]
        [InlineData(3f, 3f, 1000f, 1.5f)]
        public void EffectiveRadius_ClampsToHalfTheShortSide(float w, float h, float radius, float expected) =>
            Assert.Equal(expected, YANShape.EffectiveRadius(new RectangleF(0, 0, w, h), radius));

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(0f, 10f)]
        [InlineData(10f, 0f)]
        [InlineData(-5f, 10f)]
        public void RoundedRect_EmptyRectangle_IsNull(float w, float h) => Assert.Null(YANShape.RoundedRect(new RectangleF(0, 0, w, h), 10));

        [Fact]
        public void RoundedRect_TinyRadius_IsPlainRectangle()
        {
            using var path = YANShape.RoundedRect(new RectangleF(0, 0, 50, 20), 0.4f);
            Assert.Equal(4, path.PointCount);
            Assert.Equal(new RectangleF(0, 0, 50, 20), path.GetBounds());
        }

        [Theory]
        [InlineData(10f)]
        [InlineData(1000f)]
        public void RoundedRect_StaysInsideTheRectangle(float radius)
        {
            var rect = new RectangleF(2, 3, 60, 20);
            using var path = YANShape.RoundedRect(rect, radius);
            Assert.True(path.PointCount > 4, "arcs expected");
            var bounds = path.GetBounds();
            Assert.True(rect.Contains(RectangleF.Inflate(bounds, -0.01f, -0.01f)), $"path bounds {bounds} outside {rect}");
            // no arc of zero or negative size: the path fills without GDI+ errors
            using var bmp = new Bitmap(70, 30);
            using var g = Graphics.FromImage(bmp);
            using var brush = new LinearGradientBrush(rect, Color.Red, Color.Blue, 90f);
            g.FillPath(brush, path);
        }

        // On .NET Framework the Control.Region setter disposes the previous region itself when the value changes, so this
        // documents the behaviour; SetRegion_SameRegion_KeepsItAlive covers the branch that belongs to YANShape
        [Fact]
        public void SetRegion_ReplacesAndClearsTheRegion() => Sta.Run(() =>
        {
            using var c = new Panel { Size = new Size(50, 50) };
            var first = new Region(new Rectangle(0, 0, 10, 10));
            YANShape.SetRegion(c, first);
            Assert.Same(first, c.Region);
            using (var path = YANShape.RoundedRect(new RectangleF(0, 0, 50, 50), 10))
            {
                YANShape.SetRegion(c, path);
            }
            Assert.NotNull(c.Region);
            Assert.NotSame(first, c.Region);
            Assert.True(Gdi.IsDisposed(first), "the replaced region was not disposed");
            YANShape.SetRegion(c, (GraphicsPath)null);
            Assert.Null(c.Region);
        });

        // Assigning the current region again must not dispose it (a control would be left with a dead region)
        [Fact]
        public void SetRegion_SameRegion_KeepsItAlive() => Sta.Run(() =>
        {
            using var c = new Panel { Size = new Size(50, 50) };
            var r = new Region(new Rectangle(0, 0, 10, 10));
            YANShape.SetRegion(c, r);
            YANShape.SetRegion(c, r);
            Assert.Same(r, c.Region);
            Assert.False(Gdi.IsDisposed(r), "the current region was disposed");
        });

        [Fact]
        public void SetRoundRegion_ZeroCorner_RemovesTheRegion() => Sta.Run(() =>
        {
            using var frm = new Form { Size = new Size(200, 100) };
            YANShape.SetRegion(frm, new Region(new Rectangle(0, 0, 10, 10)));
            YANShape.SetRoundRegion(frm, 0);
            Assert.Null(frm.Region);
        });

        [Fact]
        public void SetRoundRegion_Corner_SetsARegion() => Sta.Run(() =>
        {
            using var frm = new Form { Size = new Size(200, 100) };
            YANShape.SetRoundRegion(frm, 20);
            Assert.NotNull(frm.Region);
            var before = frm.Region;
            YANShape.SetRoundRegion(frm, 30);
            Assert.NotSame(before, frm.Region);
        });
    }
}
