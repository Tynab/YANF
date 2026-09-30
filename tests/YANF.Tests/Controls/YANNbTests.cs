using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
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

        // 1.1 adds the protected virtual OnValueChanged. 2.0 (breaking): the sender is the YANNb, for a value set by code and for one
        // spun or typed in the inner NumericUpDown (1.x passed the inner NumericUpDown)
        [Fact]
        public void OnValueChanged_RaisesValueChangedWithThisAsTheSender() => Sta.Run(() =>
        {
            using var n = new NbProbe();
            var nud = Priv.Field<NumericUpDown>(n, "_nudNum");
            var senders = new List<object>();
            n.ValueChanged += (s, e) => senders.Add(s);
            n.Value = 5;
            nud.UpButton();
            // unchanged value: nothing raised
            n.Value = 6;
            Assert.Equal(2, n.Calls);
            Assert.Equal(2, senders.Count);
            Assert.All(senders, s => Assert.Same(n, s));
            Assert.Equal(6m.ToString(), n.String);
            // a handler written for 1.x that reads the value through the sender now reads the YANNb
            decimal? read = null;
            n.ValueChanged += (s, e) => read = ((YANNb)s).Value;
            nud.Value = 9;
            Assert.Equal(9m, read);
        });

        [Fact]
        public void ReadOnly_PassThrough() => Sta.Run(() =>
        {
            using var n = new YANNb();
            var nud = Priv.Field<NumericUpDown>(n, "_nudNum");
            var p = TypeDescriptor.GetProperties(n)[nameof(YANNb.ReadOnly)];
            Assert.False(n.ReadOnly);
            Assert.False(p.ShouldSerializeValue(n));
            n.ReadOnly = true;
            Assert.True(nud.ReadOnly);
            Assert.True(p.ShouldSerializeValue(n));
            // the buttons still change the value
            nud.UpButton();
            Assert.Equal(1m, n.Value);
            n.ReadOnly = false;
            Assert.False(nud.ReadOnly);
        });

        // Value is the property the Data Sources window and the (DataBindings) section bind
        [Fact]
        public void Value_IsDefaultBindingProperty() => Sta.Run(() =>
        {
            using var n = new YANNb();
            Assert.Equal(nameof(YANNb.Value), TypeDescriptor.GetAttributes(n).OfType<DefaultBindingPropertyAttribute>().Single().Name);
            Assert.Equal(BindableAttribute.Yes, TypeDescriptor.GetProperties(n)[nameof(YANNb.Value)].Attributes[typeof(BindableAttribute)]);
        });

        // Screen readers announce the inner NumericUpDown, which receives the focus: it gets the accessible name of the control
        [Fact]
        public void AccessibleName_ForwardedToInnerNumeric() => Sta.Run(ui =>
        {
            var n = new YANNb { AccessibleName = "Quantity", AccessibleDescription = "Items to order" };
            var nud = Priv.Field<NumericUpDown>(n, "_nudNum");
            // OnCreateControl runs when the control is created on a shown form
            ui.Show(n);
            Assert.Equal("Quantity", nud.AccessibleName);
            Assert.Equal("Items to order", nud.AccessibleDescription);
            // changed later: picked up when the control is entered, before it is announced
            n.AccessibleName = "Amount";
            Priv.Call(n, "Nud_Enter", nud, EventArgs.Empty);
            Assert.Equal("Amount", nud.AccessibleName);
            // a value set on the inner control directly is never overwritten
            nud.AccessibleName = "Own name";
            n.AccessibleName = "Other";
            Priv.Call(n, "Nud_Enter", nud, EventArgs.Empty);
            Assert.Equal("Own name", nud.AccessibleName);
            Assert.Equal("Items to order", nud.AccessibleDescription);
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

        // 1.0.2 assigned a new Region in OnPaint (repaint loop). 2.0: the control has no region at all (its rounded corners are painted
        // over the parent); only the inner NumericUpDown gets one, when the corners are big enough to cut it, built with the shape
        [Fact]
        public void Region_OnlyOnTheInnerNumeric_NotRebuiltByPaint() => Sta.Run(ui =>
        {
            var n = new YANNb { Size = new Size(200, 40), BorderRadius = 20, BorderSize = 2 };
            var nud = Priv.Field<NumericUpDown>(n, "_nudNum");
            ui.Draw(n);
            // the height fits the font once created: a radius of 20 clamps to half of it
            var cuts = n.Height / 2 > 15;
            Assert.Null(n.Region);
            var region = nud.Region;
            Assert.Equal(cuts, region != null);
            ui.Draw(n);
            Assert.Same(region, nud.Region);
            Assert.Null(n.Region);
            n.Height = 60;
            Assert.NotNull(nud.Region);
            n.BorderRadius = 5;
            Assert.Null(nud.Region);
            Assert.Null(n.Region);
        });

        // Counts the calls of the protected virtual OnValueChanged
        private sealed class NbProbe : YANNb
        {
            public int Calls { get; private set; }

            protected override void OnValueChanged(EventArgs e)
            {
                Calls++;
                base.OnValueChanged(e);
            }
        }
    }
}
