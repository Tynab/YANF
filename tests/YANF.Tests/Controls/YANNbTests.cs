using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANNbTests
    {
        // The designer writes Maximum before Minimum; 1.0.2 assigned the value before the bounds and threw for a negative range
        [Fact]
        public void DesignerOrder_NegativeRange() => Sta.Run(() =>
        {
            using var n = new YANNb();
            n.Maximum = -10;
            n.Minimum = -100;
            Assert.Equal((-100m, -10m), (n.Minimum, n.Maximum));
            Assert.Equal(-10m, n.Value);
            Assert.Equal((-10m).ToString(), n.String);
            n.Value = -50;
            Assert.Equal(-50m, n.Value);
            Assert.Equal((-50m).ToString(), n.String);
        });

        [Fact]
        public void DesignerOrder_RangeAboveDefaultMaximum() => Sta.Run(() =>
        {
            // designer order: Maximum, Minimum, Value
            using (var n = new YANNb())
            {
                n.Maximum = 1000;
                n.Minimum = 500;
                n.Value = 750;
                Assert.Equal((500m, 1000m, 750m), (n.Minimum, n.Maximum, n.Value));
                Assert.Equal(750m.ToString(), n.String);
            }
            // hand-written order: a Minimum above the default Maximum (100) first
            using (var n = new YANNb())
            {
                n.Minimum = 500;
                Assert.Equal((500m, 500m), (n.Maximum, n.Value));
                n.Maximum = 1000;
                Assert.Equal((500m, 1000m, 500m), (n.Minimum, n.Maximum, n.Value));
                Assert.Equal(500m.ToString(), n.String);
            }
        });

        [Fact]
        public void ValueOutsideRange_IsClamped() => Sta.Run(() =>
        {
            using var n = new YANNb();
            n.Value = 1000;
            Assert.Equal(100m, n.Value);
            Assert.Equal(100m.ToString(), n.String);
            n.Value = -5;
            Assert.Equal(0m, n.Value);
            Assert.Equal(0m.ToString(), n.String);
            n.Maximum = -10;
            n.Minimum = -100;
            n.Value = 5;
            Assert.Equal(-10m, n.Value);
            Assert.Equal((-10m).ToString(), n.String);
        });

        // A user spin or typed value only goes through the inner NumericUpDown: String must follow it
        [Fact]
        public void String_FollowsInnerValue() => Sta.Run(() =>
        {
            using var n = new YANNb();
            Assert.Equal(0m.ToString(), n.String);
            var changes = 0;
            n.ValueChanged += (s, e) => changes++;
            var nud = Priv.Field<NumericUpDown>(n, "_nudNum");
            nud.Value = 42;
            Assert.Equal(42m.ToString(), n.String);
            nud.UpButton();
            Assert.Equal(43m, n.Value);
            Assert.Equal(43m.ToString(), n.String);
            Assert.Equal(2, changes);
            // a stale public field is overwritten by the next value assignment
            n.String = "junk";
            n.Value = n.Value;
            Assert.Equal(43m.ToString(), n.String);
        });

        [Fact]
        public void Setters_NormaliseAndResizeKeepsValues() => Sta.Run(() =>
        {
            using var n = new YANNb { BorderRadius = -5, BorderSize = -3 };
            Assert.Equal((0, 0), (n.BorderRadius, n.BorderSize));
            n.BorderRadius = 20;
            n.BorderSize = 8;
            n.Size = new Size(20, 20);
            n.Size = new Size(200, 40);
            Assert.Equal((20, 8), (n.BorderRadius, n.BorderSize));
        });

        [Fact]
        public void AllSizesAndShapes_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.CompositeSizes)
            {
                foreach (var (radius, border) in Sweep.CompositeShapes)
                {
                    foreach (var underline in new[] { false, true })
                    {
                        ui.Case($"YANNb {size.Width}x{size.Height} r={radius} b={border} underline={underline}", () =>
                            ui.DrawAndDispose(new YANNb { MinimumSize = Size.Empty, Size = size, BorderRadius = radius, BorderSize = border, UnderlinedStyle = underline }));
                    }
                }
            }
            // without a parent, Parent.BackColor must not be dereferenced
            using var free = new YANNb { BorderRadius = 10, Size = new Size(200, 40) };
            ui.DrawDetached(free);
        });

        // 1.0.2 assigned a new Region in OnPaint (repaint loop); regions now change with the shape only
        [Fact]
        public void Region_NotRebuiltByPaint() => Sta.Run(ui =>
        {
            var n = new YANNb { Size = new Size(200, 40), BorderRadius = 20, BorderSize = 2 };
            ui.Draw(n);
            var region = n.Region;
            Assert.NotNull(region);
            var nud = Priv.Field<NumericUpDown>(n, "_nudNum");
            Assert.NotNull(nud.Region);
            ui.Draw(n);
            Assert.Same(region, n.Region);
            n.BorderRadius = 5;
            Assert.NotNull(n.Region);
            Assert.NotSame(region, n.Region);
            Assert.Null(nud.Region);
            n.BorderRadius = 0;
            Assert.Null(n.Region);
        });
    }
}
