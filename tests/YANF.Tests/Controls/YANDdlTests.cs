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
        // Control.BackColor (which holds the border color)
        [Fact]
        public void Focus_HighlightsBorderAndIcon() => Sta.Run(ui =>
        {
            var d = new YANDdl { BorderSize = 3 };
            ui.Draw(d);
            Assert.Equal(Color.MediumSlateBlue.ToArgb(), BorderPixel(d));
            Assert.Equal(0, PinkIconPixels(d));
            Priv.Call(d, "Ddl_Enter", d, EventArgs.Empty);
            Assert.Equal(Color.HotPink.ToArgb(), BorderPixel(d));
            Assert.True(PinkIconPixels(d) > 0, "icon not highlighted");
            Assert.Equal(Color.MediumSlateBlue, d.BorderColor);
            Assert.Equal(Color.MediumSlateBlue, d.IconColor);
            Assert.Equal(Color.MediumSlateBlue, ((System.Windows.Forms.Control)d).BackColor);
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

        // The inner combo box
        private static ComboBox Inner(YANDdl d) => Priv.Field<ComboBox>(d, "_cmbList");

        // The color of the background (the border) at a border pixel, painted through the control's own OnPaintBackground
        private static int BorderPixel(YANDdl d)
        {
            using var bmp = new Bitmap(d.Width, d.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                Priv.Call(d, "OnPaintBackground", new PaintEventArgs(g, d.ClientRectangle));
            }
            return bmp.GetPixel(1, 1).ToArgb();
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
