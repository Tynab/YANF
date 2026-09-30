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
    public class YANDdlTests
    {
        // 1.0.2 tested the old value in the setter, so Simple slipped through and later values were ignored
        [Fact]
        public void DropDownStyle_SimpleIsIgnored() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            Assert.Equal(ComboBoxStyle.DropDown, d.DropDownStyle);
            d.DropDownStyle = ComboBoxStyle.Simple;
            Assert.Equal(ComboBoxStyle.DropDown, d.DropDownStyle);
            d.DropDownStyle = ComboBoxStyle.DropDownList;
            Assert.Equal(ComboBoxStyle.DropDownList, d.DropDownStyle);
            d.DropDownStyle = ComboBoxStyle.DropDown;
            Assert.Equal(ComboBoxStyle.DropDown, d.DropDownStyle);
            ui.DrawAndDispose(d);
        });

        // A [DefaultValue] that differs from the constructor makes the designer drop real values (1.0.2: AutoCompleteMode None was lost)
        [Theory]
        [InlineData(nameof(YANDdl.AutoCompleteMode))]
        [InlineData(nameof(YANDdl.AutoCompleteSource))]
        [InlineData(nameof(YANDdl.DropDownStyle))]
        [InlineData(nameof(YANDdl.BorderFocusColor))]
        [InlineData(nameof(YANDdl.IconFocusColor))]
        [InlineData(nameof(YANDdl.BackColor))]
        [InlineData(nameof(YANDdl.BorderColor))]
        [InlineData(nameof(YANDdl.BorderSize))]
        public void DefaultValue_MatchesConstructor(string name) => Sta.Run(() =>
        {
            using var d = new YANDdl();
            var p = TypeDescriptor.GetProperties(d)[name];
            var dv = p.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
            Assert.NotNull(dv);
            Assert.Equal(dv.Value, p.GetValue(d));
            Assert.False(p.ShouldSerializeValue(d), name + " is serialized at its constructor value");
        });

        [Fact]
        public void AutoCompleteMode_None_IsSerialized() => Sta.Run(() =>
        {
            using var d = new YANDdl { AutoCompleteMode = AutoCompleteMode.None };
            Assert.True(TypeDescriptor.GetProperties(d)[nameof(YANDdl.AutoCompleteMode)].ShouldSerializeValue(d));
        });

        // 1.1: SelectedIndexChanged has this control as the sender; the legacy OnSelectedIndexChanged is still raised first, with the
        // inner ComboBox as in 1.0, and the prompt label still follows the selection
        [Fact]
        public void SelectedIndexChanged_SenderIsThis_LegacyEventUnchanged() => Sta.Run(ui =>
        {
            var d = new YANDdl { DropDownStyle = ComboBoxStyle.DropDownList };
            d.Items.AddRange(new object[] { "One", "Two", "Three" });
            ui.Draw(d);
            var cmb = Inner(d);
            var events = new List<(string Name, object Sender)>();
            d.SelectedIndexChanged += (s, e) => events.Add(("SelectedIndexChanged", s));
            d.OnSelectedIndexChanged += (s, e) => events.Add(("OnSelectedIndexChanged", s));
            d.SelectedIndex = 1;
            Assert.Equal(new[] { ("OnSelectedIndexChanged", (object)cmb), ("SelectedIndexChanged", d) }, events);
            Assert.Equal("Two", d.Text);
            Assert.Equal("Two", d.String);
            // a selection made in the inner list raises the same events
            cmb.SelectedIndex = 2;
            Assert.Equal(4, events.Count);
            Assert.Equal("Three", d.Text);
        });

        // The combo text is exposed as Text; TextChanged has this control as the sender and focus changes never raise it. String stays
        // the prompt label and the legacy StringChanged keeps the inner ComboBox as its sender
        [Fact]
        public void Text_IsTheComboText_TextChangedSenderIsThis() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            ui.Draw(d);
            var cmb = Inner(d);
            var textSenders = new List<object>();
            var stringSenders = new List<object>();
            d.TextChanged += (s, e) => textSenders.Add(s);
            d.StringChanged += (s, e) => stringSenders.Add(s);
            Assert.Equal("", d.Text);
            Assert.Equal("Select...", d.String);
            // focus changes only swap the prompt of the label
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Priv.Call(d, "Ddl_Leave", d, EventArgs.Empty);
            Assert.Empty(textSenders);
            Assert.Empty(stringSenders);
            Assert.Equal("Select...", d.String);
            // typed text (DropDown style)
            cmb.Text = "typed";
            Assert.Equal("typed", d.Text);
            Assert.Equal("typed", d.String);
            d.Text = "by code";
            Assert.Equal("by code", cmb.Text);
            Assert.Equal(new object[] { d, d }, textSenders);
            Assert.Equal(new object[] { cmb, cmb }, stringSenders);
            // String stays the prompt label and is independent of Text
            d.String = "Pick one";
            Assert.Equal("by code", d.Text);
            Assert.Equal(2, textSenders.Count);
        });

        // 1.1 lets code and bindings clear the combo text: the label then shows the prompt again instead of the old text (also before
        // the control was ever entered, and with a prompt set by the designer)
        [Fact]
        public void ClearingText_ShowsPromptAgain() => Sta.Run(ui =>
        {
            var d = new YANDdl { String = "Choose a city" };
            ui.Draw(d);
            d.Text = "Hanoi";
            Assert.Equal("Hanoi", d.String);
            d.Text = "";
            Assert.Equal("", d.Text);
            Assert.Equal("Choose a city", d.String);
            d.Text = "Hue";
            d.Text = null;
            Assert.Equal("", d.Text);
            Assert.Equal("Choose a city", d.String);
            // the default prompt; the focus cycle still restores it
            var d2 = new YANDdl();
            ui.Draw(d2);
            d2.Text = "x";
            d2.Text = "";
            Assert.Equal("Select...", d2.String);
            Priv.Call(d2, "Ddl_Enter", d2, EventArgs.Empty);
            Priv.Call(d2, "Ddl_Leave", d2, EventArgs.Empty);
            Assert.Equal("Select...", d2.String);
        });

        // A Text binding that pushes an empty (or null) field shows the prompt, not the value of the previous record; while the
        // control has the focus the label is left empty, as while the user edits the text
        [Fact]
        public void TextBinding_EmptyValueShowsPrompt() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            // a Binding only becomes active once its control is created (shown); the shown form may give it the focus
            ui.Show(d);
            Priv.Call(d, "Ddl_Leave", d, EventArgs.Empty);
            var place = new Place { City = "Hue" };
            var binding = d.DataBindings.Add(nameof(YANDdl.Text), place, nameof(Place.City));
            Assert.Equal("Hue", d.Text);
            Assert.Equal("Hue", d.String);
            place.City = "";
            binding.ReadValue();
            Assert.Equal("", d.Text);
            Assert.Equal("Select...", d.String);
            place.City = "Hanoi";
            binding.ReadValue();
            Assert.Equal("Hanoi", d.String);
            place.City = null;
            binding.ReadValue();
            Assert.Equal("", d.Text);
            Assert.Equal("Select...", d.String);
            // focused
            place.City = "Hue";
            binding.ReadValue();
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Assert.Equal("", d.String);
            place.City = "Hanoi";
            binding.ReadValue();
            Assert.Equal("Hanoi", d.String);
            place.City = "";
            binding.ReadValue();
            Assert.Equal("", d.String);
            Priv.Call(d, "Ddl_Leave", d, EventArgs.Empty);
            Assert.Equal("Select...", d.String);
        });

        // While the user edits the text, deleting all of it leaves the label empty (1.0.2 kept the last deleted character); the prompt
        // comes back when the control is left
        [Fact]
        public void TypedTextCleared_WhileFocused_LabelEmpty() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            ui.Draw(d);
            var cmb = Inner(d);
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Assert.Equal("", d.String);
            cmb.Text = "ab";
            Assert.Equal("ab", d.String);
            cmb.Text = "a";
            cmb.Text = "";
            Assert.Equal("", d.String);
            Priv.Call(d, "Ddl_Leave", d, EventArgs.Empty);
            Assert.Equal("Select...", d.String);
        });

        // DropDownList: clearing the selection shows the prompt again instead of an empty label
        [Fact]
        public void DropDownList_ClearedSelection_ShowsPrompt() => Sta.Run(ui =>
        {
            var d = new YANDdl { DropDownStyle = ComboBoxStyle.DropDownList, String = "Pick a fruit" };
            d.Items.AddRange(new object[] { "Apple", "Banana" });
            ui.Draw(d);
            d.SelectedIndex = 1;
            Assert.Equal("Banana", d.String);
            d.SelectedIndex = -1;
            Assert.Equal("", d.Text);
            Assert.Equal("Pick a fruit", d.String);
            // focus does not hide the prompt of a DropDownList
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            d.SelectedIndex = 0;
            d.SelectedIndex = -1;
            Assert.Equal("Pick a fruit", d.String);
        });

        // SelectedValue follows the rules of ComboBox: null and not settable without a DataSource, the item itself with an empty ValueMember
        [Fact]
        public void SelectedValue_ComboBoxRules() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            d.Items.AddRange(new object[] { "One", "Two" });
            ui.Show(d);
            d.SelectedIndex = 1;
            Assert.Null(d.SelectedValue);
            d.SelectedValue = "One";
            Assert.Equal(1, d.SelectedIndex);
            var d2 = new YANDdl();
            ui.Show(d2);
            d2.DataSource = new List<string> { "A", "B" };
            d2.SelectedIndex = 1;
            Assert.Equal("B", d2.SelectedValue);
        });

        // With a DataSource and an empty ValueMember, setting SelectedValue throws InvalidOperationException, as for a ComboBox
        [Fact]
        public void SelectedValue_EmptyValueMember_ThrowsLikeComboBox() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            ui.Show(d);
            d.DataSource = new List<string> { "A", "B" };
            Assert.Throws<InvalidOperationException>(() => d.SelectedValue = "A");
        });

        // Items bound from a list of objects: SelectedValue follows ValueMember, SelectedValueChanged has this control as the sender,
        // and SelectedValue can be data-bound in both directions
        [Fact]
        public void DataSource_SelectedValue_BindsListOfObjects() => Sta.Run(ui =>
        {
            var fruits = new List<Fruit> { new(10, "Apple"), new(20, "Banana"), new(30, "Cherry") };
            var d = new YANDdl { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Fruit.Name), ValueMember = nameof(Fruit.Id) };
            // a Binding only becomes active once its control is created (shown)
            ui.Show(d);
            d.DataSource = fruits;
            Assert.Equal(3, Inner(d).Items.Count);
            var indexSenders = new List<object>();
            var valueSenders = new List<object>();
            d.SelectedIndexChanged += (s, e) => indexSenders.Add(s);
            d.SelectedValueChanged += (s, e) => valueSenders.Add(s);
            d.SelectedValue = 20;
            Assert.Equal(1, d.SelectedIndex);
            Assert.Same(fruits[1], d.SelectedItem);
            Assert.Equal("Banana", d.Text);
            Assert.Equal("Banana", d.String);
            Assert.NotEmpty(indexSenders);
            Assert.NotEmpty(valueSenders);
            Assert.All(indexSenders.Concat(valueSenders), s => Assert.Same(d, s));
            d.SelectedIndex = 2;
            Assert.Equal(30, d.SelectedValue);
            // binding: SelectedValue <-> Order.FruitId
            var order = new Order { FruitId = 10 };
            var binding = d.DataBindings.Add(nameof(YANDdl.SelectedValue), order, nameof(Order.FruitId), false, DataSourceUpdateMode.OnPropertyChanged);
            Assert.Equal(10, d.SelectedValue);
            Assert.Equal(0, d.SelectedIndex);
            d.SelectedIndex = 1;
            Assert.Equal(20, order.FruitId);
            order.FruitId = 30;
            binding.ReadValue();
            Assert.Equal(2, d.SelectedIndex);
            Assert.Equal("Cherry", d.Text);
        });

        // The Data Sources window and the designer find the lookup and default binding properties and the new default event; the
        // legacy events are hidden but still work, and the new data properties are never written by the designer
        [Fact]
        public void DesignerMetadata_BindingAndEvents() => Sta.Run(() =>
        {
            using var d = new YANDdl();
            var lookup = TypeDescriptor.GetAttributes(d).OfType<LookupBindingPropertiesAttribute>().Single();
            Assert.Equal((nameof(YANDdl.DataSource), nameof(YANDdl.DisplayMember), nameof(YANDdl.ValueMember), nameof(YANDdl.SelectedValue)),
                (lookup.DataSource, lookup.DisplayMember, lookup.ValueMember, lookup.LookupMember));
            Assert.Equal(nameof(YANDdl.Text), TypeDescriptor.GetAttributes(d).OfType<DefaultBindingPropertyAttribute>().Single().Name);
            Assert.Equal(nameof(YANDdl.SelectedIndexChanged), TypeDescriptor.GetDefaultEvent(d).Name);
            var events = TypeDescriptor.GetEvents(d);
            Assert.True(events[nameof(YANDdl.SelectedIndexChanged)].IsBrowsable);
            Assert.True(events[nameof(YANDdl.SelectedValueChanged)].IsBrowsable);
            Assert.True(events[nameof(YANDdl.TextChanged)].IsBrowsable);
            Assert.False(events[nameof(YANDdl.OnSelectedIndexChanged)].IsBrowsable);
            Assert.False(events[nameof(YANDdl.StringChanged)].IsBrowsable);
            var props = TypeDescriptor.GetProperties(d);
            foreach (var name in new[] { nameof(YANDdl.SelectedValue), nameof(YANDdl.Text) })
            {
                Assert.Equal(BindableAttribute.Yes, props[name].Attributes[typeof(BindableAttribute)]);
                Assert.Equal(DesignerSerializationVisibility.Hidden, props[name].SerializationVisibility);
            }
            // the prompt is still a designer property
            Assert.True(props[nameof(YANDdl.String)].IsBrowsable);
            Assert.Equal(DesignerSerializationVisibility.Visible, props[nameof(YANDdl.String)].SerializationVisibility);
        });

        // The border and the icon are highlighted while the control has the focus, without changing BorderColor, IconColor or
        // BackColor (2.0: Control.BackColor is the surface, it no longer holds the border color)
        [Fact]
        public void Focus_HighlightsBorderAndIcon() => Sta.Run(ui =>
        {
            var d = new YANDdl { BorderSize = 3 };
            ui.Draw(d);
            Assert.Equal(Color.MediumSlateBlue.ToArgb(), BorderPixel(d));
            Assert.Equal(0, PinkIconPixels(d));
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Assert.Equal(Color.HotPink.ToArgb(), BorderPixel(d));
            Assert.Equal(Color.WhiteSmoke.ToArgb(), SurfacePixel(d));
            Assert.True(PinkIconPixels(d) > 0, "icon not highlighted");
            Assert.Equal(Color.MediumSlateBlue, d.BorderColor);
            Assert.Equal(Color.MediumSlateBlue, d.IconColor);
            Assert.Equal(Color.WhiteSmoke, ((System.Windows.Forms.Control)d).BackColor);
            Priv.Call(d, "Ddl_Leave", d, EventArgs.Empty);
            Assert.Equal(Color.MediumSlateBlue.ToArgb(), BorderPixel(d));
            Assert.Equal(0, PinkIconPixels(d));
            // an empty focus color turns the highlight off
            d.BorderFocusColor = Color.Empty;
            d.IconFocusColor = Color.Empty;
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Assert.Equal(Color.MediumSlateBlue.ToArgb(), BorderPixel(d));
            Assert.Equal(0, PinkIconPixels(d));
        });

        // Screen readers announce the inner ComboBox, which receives the focus: it gets the accessible name of the control
        [Fact]
        public void AccessibleName_ForwardedToInnerCombo() => Sta.Run(ui =>
        {
            var d = new YANDdl { AccessibleName = "Country", AccessibleDescription = "Country of residence" };
            var cmb = Inner(d);
            // OnCreateControl runs when the control is created on a shown form
            ui.Show(d);
            Assert.Equal("Country", cmb.AccessibleName);
            Assert.Equal("Country of residence", cmb.AccessibleDescription);
            // changed later: picked up when the control is entered, before it is announced
            d.AccessibleName = "Nationality";
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Assert.Equal("Nationality", cmb.AccessibleName);
            // a value set on the inner combo box directly is never overwritten
            cmb.AccessibleName = "Own name";
            d.AccessibleName = "Other";
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Assert.Equal("Own name", cmb.AccessibleName);
        });

        [Fact]
        public void Resize_KeepsBorderSize() => Sta.Run(ui =>
        {
            var d = new YANDdl { BorderSize = 12, MinimumSize = Size.Empty };
            d.Size = new Size(10, 10);
            d.Size = new Size(200, 35);
            Assert.Equal(12, d.BorderSize);
            d.BorderSize = -4;
            Assert.Equal(0, d.BorderSize);
            Assert.Equal(new Padding(0), d.Padding);
            ui.DrawAndDispose(d);
        });

        #region 2.0 painting and BackColor
        // 2.0 (breaking): BackColor overrides Control.BackColor and is the surface. Code that colors controls through a Control
        // reference (a theme loop) colors the surface, not the border, and BorderColor no longer changes Control.BackColor
        [Fact]
        public void BackColor_IsTheSurface_AlsoThroughAControlReference() => Sta.Run(ui =>
        {
            var d = new YANDdl { BorderSize = 3 };
            ui.Draw(d);
            var lbl = Priv.Field<Label>(d, "_lblText");
            var btn = Priv.Field<Button>(d, "_btnIc");
            var changes = 0;
            d.BackColorChanged += (s, e) => changes++;
            System.Windows.Forms.Control c = d;
            c.BackColor = Color.Red;
            Assert.Equal(Color.Red, d.BackColor);
            Assert.Equal((Color.Red, Color.Red), (lbl.BackColor, btn.BackColor));
            Assert.Equal(Color.MediumSlateBlue, d.BorderColor);
            Assert.Equal(1, changes);
            Assert.Equal(Color.Red.ToArgb(), SurfacePixel(d));
            Assert.Equal(Color.MediumSlateBlue.ToArgb(), BorderPixel(d));
            // the typed property is the same property
            d.BackColor = Color.Blue;
            Assert.Equal(Color.Blue, c.BackColor);
            Assert.Equal(Color.Blue, lbl.BackColor);
            d.BorderColor = Color.Lime;
            Assert.Equal(Color.Blue, c.BackColor);
            Assert.Equal(2, changes);
            Assert.Equal(Color.Lime.ToArgb(), BorderPixel(d));
            Assert.Equal(Color.Blue.ToArgb(), SurfacePixel(d));
            // the designer writes BackColor only when it differs from WhiteSmoke, as in 1.x
            var p = TypeDescriptor.GetProperties(d)[nameof(YANDdl.BackColor)];
            Assert.True(p.ShouldSerializeValue(d));
            p.ResetValue(d);
            Assert.Equal(Color.WhiteSmoke, d.BackColor);
            Assert.False(p.ShouldSerializeValue(d));
        });

        // What a 1.x designer file writes keeps its look at 96 dpi: the border in BorderColor, BorderSize wide, and the surface in BackColor
        [Fact]
        public void DesignerFile_KeepsItsLook() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            var pnl = new PaintingTests.LimePanel();
            ui.Host.Controls.Add(pnl);
            // MainFrm.Designer.cs (yanDdl2), in the designer's order
            d.BackColor = Color.FromArgb(37, 42, 64);
            d.BorderColor = Color.MediumSlateBlue;
            d.BorderSize = 1;
            d.Location = new Point(12, 58);
            d.MinimumSize = new Size(200, 30);
            d.Padding = new Padding(1);
            d.Size = new Size(200, 35);
            pnl.Controls.Add(d);
            using var bmp = ui.Render(d);
            foreach (var p in new[] { new Point(0, 0), new Point(100, 0), new Point(0, 17), new Point(199, 17), new Point(100, 34), new Point(199, 34) })
            {
                PaintingTests.AssertColor(Color.MediumSlateBlue, bmp.GetPixel(p.X, p.Y), $"border at {p}");
            }
            Assert.Equal(Color.FromArgb(37, 42, 64).ToArgb(), SurfacePixel(d, new Point(3, 3)));
            Assert.Equal(Color.FromArgb(37, 42, 64), Priv.Field<Label>(d, "_lblText").BackColor);
            Assert.Equal(new Padding(1), d.Padding);
        });

        // A transparent surface and border show the parent's own background (Demo1's yanDdl1: both Transparent, Padding(25, 0, 0, 0))
        [Fact]
        public void Transparent_ShowsTheParent() => Sta.Run(ui =>
        {
            var pnl = new PaintingTests.LimePanel();
            var paints = 0;
            pnl.Paint += (s, e) => paints++;
            ui.Host.Controls.Add(pnl);
            var d = new YANDdl { Location = new Point(10, 10) };
            pnl.Controls.Add(d);
            using (ui.Render(d))
            {
            }
            // an opaque control hides the parent: it is not painted behind it
            Assert.Equal(0, paints);
            d.BackColor = Color.Transparent;
            d.BorderColor = Color.Transparent;
            d.BorderSize = 0;
            d.Padding = new Padding(25, 0, 0, 0);
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(0, 0), "corner");
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(12, d.Height / 2), "left padding");
            }
            Assert.True(paints > 0, "the parent is not painted behind the transparent surface");
            // a transparent border over an opaque surface
            d.BackColor = Color.Blue;
            d.BorderSize = 2;
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(0, d.Height / 2), "transparent border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(12, d.Height / 2), "surface");
            }
        });

        // As in 1.x, the border fills the whole band outside the Padding (the label and the icon cover the rest), also when a designer file
        // sets a Padding wider than BorderSize (Demo1's yanDdl1: Padding(25, 0, 0, 0)); 1.1's focus color covers the whole band too
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void WiderPadding_IsPaintedAsTheBorder_AsIn1x(int border) => Sta.Run(ui =>
        {
            var pnl = new PaintingTests.LimePanel();
            ui.Host.Controls.Add(pnl);
            // in the designer's order: BorderSize sets the Padding, then the designer file sets its own
            var d = new YANDdl { BackColor = Color.Blue, BorderColor = Color.Red, BorderSize = border, Location = new Point(10, 10) };
            d.Padding = new Padding(25, 0, 0, 0);
            pnl.Controls.Add(d);
            var y = d.Height / 2;
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(12, y), "left padding");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(24, y), "last column of the left padding");
            }
            Assert.Equal(Color.Red.ToArgb(), BackgroundPixel(d, new Point(12, y)));
            Assert.Equal(Color.Blue.ToArgb(), BackgroundPixel(d, new Point(25, y)));
            // the label covers the rest from the Padding on (Padding.Top is 0: no border shows at the top, as in 1.x)
            Assert.Equal(new Rectangle(25, 0, d.Width - 25 - 30, d.Height), Priv.Field<Label>(d, "_lblText").Bounds);
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.HotPink, bmp.GetPixel(12, y), "left padding with the focus");
            }
            Assert.Equal(Color.HotPink.ToArgb(), BackgroundPixel(d, new Point(12, y)));
            Assert.Equal(Color.Blue.ToArgb(), BackgroundPixel(d, new Point(25, y)));
            Priv.Call(d, "Ddl_Leave", d, EventArgs.Empty);
            // Demo1's yanDdl1: a transparent border shows the parent, the focus color covers it
            d.BackColor = Color.Transparent;
            d.BorderColor = Color.Transparent;
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(12, y), "transparent left padding");
            }
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            using (var bmp = ui.Render(d))
            {
                PaintingTests.AssertColor(Color.HotPink, bmp.GetPixel(12, y), "transparent left padding with the focus");
            }
        });

        // A Padding wider than the control leaves no surface: the border covers everything, nothing throws
        [Fact]
        public void PaddingWiderThanTheControl_IsAllBorder() => Sta.Run(ui =>
        {
            var d = new YANDdl { BackColor = Color.Blue, BorderColor = Color.Red };
            ui.Draw(d);
            d.Padding = new Padding(d.Width, 0, 0, 0);
            Assert.Equal(Color.Red.ToArgb(), BackgroundPixel(d, new Point(d.Width / 2, d.Height / 2)));
            d.BorderColor = Color.Transparent;
            ui.DrawAndDispose(d);
        });

        // A BackgroundImage is painted on the surface, inside the border
        [Fact]
        public void BackgroundImage_IsPaintedInsideTheBorder() => Sta.Run(ui =>
        {
            using var img = new Bitmap(8, 8);
            using (var g = Graphics.FromImage(img))
            {
                g.Clear(Color.Yellow);
            }
            var d = new YANDdl { BorderSize = 3, BorderColor = Color.Red, BackgroundImage = img };
            ui.Draw(d);
            Assert.Equal(Color.Red.ToArgb(), BorderPixel(d));
            Assert.Equal(Color.Yellow.ToArgb(), SurfacePixel(d, new Point(5, 5)));
        });

        // A translucent focus color is painted over BorderColor, as in 1.x
        [Fact]
        public void TranslucentFocusColor_OverTheBorderColor() => Sta.Run(ui =>
        {
            var d = new YANDdl { BorderSize = 3, BorderColor = Color.Blue, BorderFocusColor = Color.FromArgb(128, Color.Red) };
            ui.Draw(d);
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            var px = Color.FromArgb(BorderPixel(d));
            Assert.True(px.R > 100 && px.B > 100 && px.G < 30, $"expected red over blue, got {px}");
        });

        // BorderSize is in 96-dpi pixels: at 192 dpi the border and the padding that keeps the text and the icon inside it are twice as
        // wide, and so is the arrow of the icon
        [Fact]
        public void Dpi_ScalesTheBorderThePaddingAndTheArrow() => Sta.Run(ui =>
        {
            var d = new YANDdl { BorderColor = Color.Red };
            ui.Draw(d);
            var arrow96 = PinkIconPixels(d, true);
            Assert.Equal(new Padding(1), d.Padding);
            YANPaint.DpiOverride = 192;
            try
            {
                d.BorderSize = 1;
                Assert.Equal(new Padding(2), d.Padding);
                Assert.Equal(1, d.BorderSize);
                Assert.Equal(Color.Red.ToArgb(), BorderPixel(d));
                Assert.Equal(Color.Red.ToArgb(), BackgroundPixel(d, new Point(d.Width / 2, 1)));
                Assert.Equal(Color.WhiteSmoke.ToArgb(), SurfacePixel(d, new Point(4, 4)));
                Assert.True(PinkIconPixels(d, true) > arrow96 * 2, $"arrow not scaled ({PinkIconPixels(d, true)} vs {arrow96} pixels)");
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // High contrast mode paints the border with the system frame color (the highlight color while the control has the focus)
        [Fact]
        public void HighContrast_BorderUsesSystemColors() => Sta.Run(ui =>
        {
            YANPaint.HighContrastOverride = true;
            try
            {
                var d = new YANDdl { BorderSize = 3, BorderColor = Color.Red };
                ui.Draw(d);
                Assert.Equal(SystemColors.WindowFrame.ToArgb(), BorderPixel(d));
                Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
                Assert.Equal(SystemColors.Highlight.ToArgb(), BorderPixel(d));
                Assert.Equal(Color.Red, d.BorderColor);
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
        });

        // Zero, tiny and huge sizes and borders, transparent or not, on a painted parent; a new parent and a new window handle
        [Fact]
        public void Sizes_ParentChange_AndHandleRecreation_Draw() => Sta.Run(ui =>
        {
            foreach (var size in new[] { new Size(0, 0), new Size(1, 1), new Size(3, 1), new Size(2000, 2000) })
            {
                foreach (var border in new[] { 0, 1, 49, 1000 })
                {
                    foreach (var back in new[] { Color.WhiteSmoke, Color.Transparent })
                    {
                        ui.Case($"YANDdl {size.Width}x{size.Height} b={border} back={back.Name}", () =>
                        {
                            var d = Sweep.Sized(new YANDdl { BorderSize = border, BackColor = back, BorderColor = back }, size);
                            var pnl = new PaintingTests.LimePanel();
                            ui.Host.Controls.Add(pnl);
                            pnl.Controls.Add(d);
                            ui.DrawAndDispose(d);
                        });
                    }
                }
            }
            var t = new YANDdl { BackColor = Color.Transparent, BorderColor = Color.Transparent };
            var lime = new PaintingTests.LimePanel();
            ui.Host.Controls.Add(lime);
            lime.Controls.Add(t);
            using (var bmp = ui.Render(t))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(0, 0), "first parent");
            }
            var red = new PaintingTests.ColorPanel(Color.Red);
            ui.Host.Controls.Add(red);
            red.Controls.Add(t);
            using (var bmp = ui.Render(t))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(0, 0), "second parent");
            }
            var handle = t.Handle;
            Priv.Call(t, "RecreateHandle");
            Assert.NotEqual(handle, t.Handle);
            using (var bmp = ui.Render(t))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(0, 0), "after RecreateHandle");
            }
            red.Controls.Remove(t);
            ui.DrawDetached(t);
            t.Dispose();
        });
        #endregion

        // The inner combo box
        private static ComboBox Inner(YANDdl d) => Priv.Field<ComboBox>(d, "_cmbList");

        // The color of the background (the border) at a border pixel, painted through the control's own OnPaintBackground
        private static int BorderPixel(YANDdl d) => BackgroundPixel(d, new Point(1, 1));

        // The color of the surface (inside the border), painted through the control's own OnPaintBackground (the inner controls cover it)
        private static int SurfacePixel(YANDdl d) => BackgroundPixel(d, new Point(d.Width / 2, d.Height / 2));

        private static int SurfacePixel(YANDdl d, Point p) => BackgroundPixel(d, p);

        private static int BackgroundPixel(YANDdl d, Point p)
        {
            using var bmp = new Bitmap(d.Width, d.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                Priv.Call(d, "OnPaintBackground", new PaintEventArgs(g, d.ClientRectangle));
            }
            return bmp.GetPixel(p.X, p.Y).ToArgb();
        }

        // The number of hot pink pixels of the arrow icon, painted through the icon button's Paint handler (focused, when asked)
        private static int PinkIconPixels(YANDdl d, bool focused)
        {
            var isFocus = Priv.Field<bool>(d, "_is_Focus");
            Priv.Call(d, focused ? "Ddl_Enter" : "Ddl_Leave", d, EventArgs.Empty);
            try
            {
                return PinkIconPixels(d);
            }
            finally
            {
                Priv.Call(d, isFocus ? "Ddl_Enter" : "Ddl_Leave", d, EventArgs.Empty);
            }
        }

        // The number of hot pink pixels of the arrow icon, painted through the icon button's Paint handler
        private static int PinkIconPixels(YANDdl d)
        {
            var btn = Priv.Field<Button>(d, "_btnIc");
            using var bmp = new Bitmap(btn.Width, btn.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                Priv.Call(d, "Ic_Paint", btn, new PaintEventArgs(g, btn.ClientRectangle));
            }
            var count = 0;
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var c = bmp.GetPixel(x, y);
                    // hot pink (255, 105, 180), allowing for anti-aliasing against white
                    if (c.R > 230 && c.G < 170 && c.B > 150 && c.B < 215)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public sealed class Fruit
        {
            public Fruit(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public int Id { get; }

            public string Name { get; }
        }

        public sealed class Place : INotifyPropertyChanged
        {
            private string _city;

            public event PropertyChangedEventHandler PropertyChanged;

            public string City
            {
                get => _city;
                set
                {
                    _city = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(City)));
                }
            }
        }

        public sealed class Order : INotifyPropertyChanged
        {
            private int _fruitId;

            public event PropertyChangedEventHandler PropertyChanged;

            public int FruitId
            {
                get => _fruitId;
                set
                {
                    _fruitId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FruitId)));
                }
            }
        }
    }
}
