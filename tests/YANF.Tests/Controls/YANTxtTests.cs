using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANTxtTests
    {
        [Fact]
        public void RadiusEqualsBorder_Draws() => Sta.Run(ui => ui.DrawAndDispose(new YANTxt
        {
            BorderSize = 2,
            BorderRadius = 2
        }));

        // The placeholder is painted, never written into the text: String is null for an empty box with a placeholder
        [Fact]
        public void String_NullWhenEmptyWithPlaceholder() => Sta.Run(() =>
        {
            using var t = new YANTxt();
            var inner = Inner(t);
            Assert.Equal("", t.String);
            t.PlaceholderText = "Hint";
            Assert.Null(t.String);
            Assert.Equal("", inner.Text);
            t.String = "   ";
            Assert.Null(t.String);
            Assert.Equal("", inner.Text);
            t.String = null;
            Assert.Null(t.String);
            Assert.Equal("", inner.Text);
            t.String = "abc";
            Assert.Equal("abc", t.String);
            // without a placeholder, white space is kept as in 1.0.2
            t.PlaceholderText = null;
            t.String = "  ";
            Assert.Equal("  ", t.String);
        });

        // 1.0.2 overwrote the text with the placeholder when PlaceholderText was set
        [Fact]
        public void PlaceholderText_KeepsText() => Sta.Run(ui =>
        {
            var t = new YANTxt();
            var changes = 0;
            t.StringChanged += (s, e) => changes++;
            t.String = "typed by the user";
            Assert.Equal(1, changes);
            t.PlaceholderText = "Hint";
            Assert.Equal("typed by the user", t.String);
            t.PlaceholderColor = Color.Red;
            t.PlaceholderText = "Other hint";
            Assert.Equal(1, changes);
            ui.Draw(t);
            t.String = null;
            Assert.Equal(2, changes);
            Assert.Null(t.String);
        });

        // 1.0.2 swapped the placeholder in and out of Text on Enter/Leave, raising StringChanged on every focus change
        [Fact]
        public void StringChanged_NotRaisedByFocusChanges() => Sta.Run(ui =>
        {
            var t = new YANTxt { PlaceholderText = "Hint" };
            var other = new TextBox();
            var changes = 0;
            var textChanges = 0;
            t.StringChanged += (s, e) => changes++;
            t.TextChanged += (s, e) => textChanges++;
            ui.Show(t, other);
            var inner = Inner(t);
            for (var i = 0; i < 3; i++)
            {
                inner.Focus();
                ui.Pump(2);
                other.Focus();
                ui.Pump(2);
            }
            // run the Enter/Leave handlers even where the window could not take the focus
            Priv.Call(t, "Txt_Enter", inner, EventArgs.Empty);
            Priv.Call(t, "Txt_Leave", inner, EventArgs.Empty);
            Assert.Equal((0, 0), (changes, textChanges));
            Assert.Equal("", inner.Text);
            Assert.Equal("", t.Text);
            Assert.Null(t.String);
            inner.Focus();
            ui.Pump(2);
            t.String = "abc";
            other.Focus();
            ui.Pump(2);
            Assert.Equal((1, 1), (changes, textChanges));
            Assert.Equal("abc", t.String);
            Assert.Equal("abc", t.Text);
        });

        // 1.0.2 removed the placeholder on Enter, so a focused box reported its raw text ("" or typed spaces), for example to an
        // Enter-to-submit KeyDown handler; that behaviour is kept
        [Fact]
        public void String_RawTextWhileFocused() => Sta.Run(ui =>
        {
            var t = new YANTxt { PlaceholderText = "Hint" };
            var other = new TextBox();
            ui.Show(t, other);
            var inner = Inner(t);
            // the shown form focuses the first control: move the focus away first
            Blur(ui, t, inner, other);
            Assert.Null(t.String);
            Focus(ui, t, inner);
            Assert.Equal("", t.String);
            string inKeyDown = "not called";
            t.KeyDown += (s, e) => inKeyDown = t.String;
            Priv.Call(t, "Txt_KeyDown", inner, new KeyEventArgs(Keys.Enter));
            Assert.Equal("", inKeyDown);
            inner.Text = "  ";
            Assert.Equal("  ", t.String);
            Blur(ui, t, inner, other);
            Assert.Null(t.String);
            Priv.Call(t, "Txt_Enter", inner, EventArgs.Empty);
            Assert.Equal("  ", t.String);
            Priv.Call(t, "Txt_Leave", inner, EventArgs.Empty);
            t.PlaceholderText = null;
            Assert.Equal("  ", t.String);
        });

        // 1.0.2 switched the password mask off to show the placeholder, so typed passwords could appear in clear text
        [Fact]
        public void PasswordChar_StaysOnWithPlaceholder() => Sta.Run(ui =>
        {
            var t = new YANTxt { PlaceholderText = "Password", PasswordChar = true };
            var inner = Inner(t);
            Assert.True(t.PasswordChar);
            Assert.True(inner.UseSystemPasswordChar);
            ui.Show(t);
            Priv.Call(t, "Txt_Enter", inner, EventArgs.Empty);
            t.String = "secret";
            Priv.Call(t, "Txt_Leave", inner, EventArgs.Empty);
            Assert.True(inner.UseSystemPasswordChar);
            t.String = null;
            Priv.Call(t, "Txt_Leave", inner, EventArgs.Empty);
            Assert.True(inner.UseSystemPasswordChar);
            Assert.True(t.PasswordChar);
            inner.Refresh();
            t.PasswordChar = false;
            Assert.False(t.PasswordChar);
            Assert.False(inner.UseSystemPasswordChar);
        });

        [Fact]
        public void Placeholder_PaintsWithoutText() => Sta.Run(ui =>
        {
            foreach (var align in new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right })
            {
                foreach (var multiline in new[] { false, true })
                {
                    ui.Case($"placeholder align={align} multiline={multiline}", () =>
                    {
                        var t = new YANTxt { PlaceholderText = "Hint", TextAlign = align, Multiline = multiline, BorderRadius = 20 };
                        using var frm = ui.Show(t);
                        var inner = Inner(t);
                        // WM_PAINT goes through CueTextBox.WndProc
                        inner.Refresh();
                        t.Refresh();
                        ui.Pump(2);
                        Assert.Equal("", inner.Text);
                    });
                }
            }
        });

        // The placeholder is drawn on screen after WM_PAINT, so this reads the screen (the form is top-most)
        [Fact]
        public void Placeholder_VisibleOnlyWhenEmptyAndUnfocused() => Sta.Run(ui =>
        {
            // a big bold font, so that whatever the text anti-aliasing, the strokes have fully colored pixels
            var t = new YANTxt
            {
                PlaceholderText = "WWWWWWWW",
                PlaceholderColor = Color.Red,
                Font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold),
                Size = new Size(300, 40),
                TextAlign = HorizontalAlignment.Left
            };
            var other = new TextBox();
            ui.Show(t, other);
            var inner = Inner(t);
            other.Focus();
            ui.Pump();
            inner.Refresh();
            ui.Pump();
            Assert.True(Red(inner) > 0, "placeholder not painted while empty and unfocused");
            inner.Focus();
            ui.Pump();
            Assert.True(!inner.Focused || Red(inner) == 0, "placeholder painted while focused");
            other.Focus();
            ui.Pump();
            Assert.True(Red(inner) > 0, "placeholder not back after leaving");
            t.String = "abc";
            ui.Pump();
            Assert.Equal("abc", inner.Text);
            Assert.True(Red(inner) == 0, "placeholder painted over text");
            t.String = null;
            ui.Pump();
            Assert.True(Red(inner) > 0, "placeholder not back after clearing");
            // typed spaces left behind: String reports empty, so the placeholder shows as in 1.0.2
            inner.Text = "   ";
            ui.Pump();
            Assert.Null(t.String);
            Assert.True(Red(inner) > 0, "placeholder not shown over whitespace-only text");
            // password mode: the placeholder covers the dots of the blank text
            t.PasswordChar = true;
            inner.Refresh();
            ui.Pump();
            Assert.True(Red(inner) > 0 && Dark(inner) == 0, $"placeholder must cover the password dots of whitespace text (red={Red(inner)}, dark={Dark(inner)})");
            inner.Text = "ab";
            ui.Pump();
            Assert.True(Red(inner) == 0 && Dark(inner) > 0, $"real password text shows its dots and no placeholder (red={Red(inner)}, dark={Dark(inner)})");
        });

        [Fact]
        public void Setters_NormaliseAndResizeKeepsValues() => Sta.Run(() =>
        {
            using var t = new YANTxt { BorderRadius = -5, BorderSize = -3 };
            Assert.Equal((0, 0), (t.BorderRadius, t.BorderSize));
            t.BorderRadius = 20;
            t.BorderSize = 8;
            t.Size = new Size(20, 20);
            t.Size = new Size(200, 40);
            Assert.Equal((20, 8), (t.BorderRadius, t.BorderSize));
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
                        foreach (var multiline in new[] { false, true })
                        {
                            ui.Case($"YANTxt {size.Width}x{size.Height} r={radius} b={border} underline={underline} multiline={multiline}", () =>
                                ui.DrawAndDispose(new YANTxt { MinimumSize = Size.Empty, Size = size, BorderRadius = radius, BorderSize = border, UnderlinedStyle = underline, Multiline = multiline, PlaceholderText = "Hint" }));
                        }
                    }
                }
            }
            // without a parent, Parent.BackColor must not be dereferenced
            using var free = new YANTxt { BorderRadius = 10, Size = new Size(200, 40) };
            ui.DrawDetached(free);
        });

        // 1.0.2 assigned a new Region in OnPaint (repaint loop). 2.0: the control has no region at all (its rounded corners are painted
        // over the parent); only the inner text box gets one, when the corners are big enough to cut it, built with the shape
        [Fact]
        public void Region_OnlyOnTheInnerBox_NotRebuiltByPaint() => Sta.Run(ui =>
        {
            var t = new YANTxt { Multiline = true, Size = new Size(200, 60), BorderRadius = 25, BorderSize = 2 };
            var inner = Inner(t);
            ui.Draw(t);
            Assert.Null(t.Region);
            var region = inner.Region;
            Assert.NotNull(region);
            ui.Draw(t);
            Assert.Same(region, inner.Region);
            Assert.Null(t.Region);
            t.BorderRadius = 5;
            Assert.Null(inner.Region);
            t.BorderRadius = 25;
            Assert.NotNull(inner.Region);
            // the radius is clamped to half the height: 10 no longer cuts the inner box
            t.Size = new Size(200, 20);
            Assert.Null(inner.Region);
            t.Size = new Size(200, 60);
            t.BorderRadius = 0;
            Assert.Null(inner.Region);
            Assert.Null(t.Region);
        });

        // 1.1: Text is the content of the box (UserControl.Text was an unrelated hidden property that always returned "");
        // TextChanged has this control as the sender, while the legacy StringChanged keeps the inner text box as in 1.0
        [Fact]
        public void Text_IsTheContent_TextChangedSenderIsThis() => Sta.Run(() =>
        {
            using var t = new YANTxt();
            var inner = Inner(t);
            var events = new List<(string Name, object Sender)>();
            t.TextChanged += (s, e) => events.Add(("TextChanged", s));
            t.StringChanged += (s, e) => events.Add(("StringChanged", s));
            Assert.Equal("", t.Text);
            t.Text = "abc";
            Assert.Equal("abc", inner.Text);
            Assert.Equal("abc", t.String);
            // typed by the user
            inner.Text = "typed";
            Assert.Equal("typed", t.Text);
            // the legacy alias writes the same text
            t.String = "legacy";
            Assert.Equal("legacy", t.Text);
            t.Text = null;
            Assert.Equal("", t.Text);
            Assert.Equal(4 * 2, events.Count);
            Assert.All(events.Where(x => x.Name == "TextChanged"), x => Assert.Same(t, x.Sender));
            Assert.All(events.Where(x => x.Name == "StringChanged"), x => Assert.Same(inner, x.Sender));
            // TextChanged comes first, then the legacy event, for each change
            Assert.Equal(Enumerable.Range(0, 4).SelectMany(_ => new[] { "TextChanged", "StringChanged" }), events.Select(x => x.Name));
        });

        // Subclasses can intercept the change through the protected virtual OnTextChanged
        [Fact]
        public void OnTextChanged_IsCalledForEveryChange() => Sta.Run(() =>
        {
            using var t = new TxtProbe();
            var raised = 0;
            t.TextChanged += (s, e) => raised++;
            t.Text = "a";
            Priv.Field<TextBox>(t, "_txtText").Text = "ab";
            Assert.Equal((2, 2), (t.Calls, raised));
        });

        // String keeps its 1.0 semantics next to Text: null while the placeholder shows, white space counts as empty
        [Fact]
        public void String_LegacySemanticsNextToText() => Sta.Run(() =>
        {
            using var t = new YANTxt { PlaceholderText = "Hint" };
            Assert.Null(t.String);
            Assert.Equal("", t.Text);
            t.Text = "   ";
            Assert.Equal("   ", t.Text);
            Assert.Null(t.String);
            t.String = "   ";
            Assert.Equal("", t.Text);
            t.String = "abc";
            Assert.Equal("abc", t.Text);
            // existing designer files set String: it still compiles and works
            using var fromDesigner = new YANTxt { PlaceholderText = null, String = "Text" };
            Assert.Equal("Text", fromDesigner.Text);
        });

        // Text is what the designer writes and binds now; String and PasswordChar still work but are hidden and no longer written
        [Fact]
        public void DesignerMetadata_TextIsMainProperty() => Sta.Run(() =>
        {
            using var t = new YANTxt();
            var props = TypeDescriptor.GetProperties(t);
            var text = props[nameof(YANTxt.Text)];
            Assert.True(text.IsBrowsable);
            Assert.True(text.IsLocalizable);
            Assert.Equal(BindableAttribute.Yes, text.Attributes[typeof(BindableAttribute)]);
            Assert.Equal(DesignerSerializationVisibility.Visible, text.SerializationVisibility);
            Assert.Equal(EditorBrowsableState.Always, text.Attributes.OfType<EditorBrowsableAttribute>().Single().State);
            Assert.False(text.ShouldSerializeValue(t));
            t.Text = "abc";
            Assert.True(text.ShouldSerializeValue(t));
            foreach (var name in new[] { nameof(YANTxt.String), nameof(YANTxt.PasswordChar) })
            {
                var legacy = props[name];
                Assert.False(legacy.IsBrowsable, name);
                Assert.Equal(DesignerSerializationVisibility.Hidden, legacy.SerializationVisibility);
                Assert.Equal(EditorBrowsableState.Never, legacy.Attributes.OfType<EditorBrowsableAttribute>().Single().State);
            }
            var events = TypeDescriptor.GetEvents(t);
            Assert.True(events[nameof(YANTxt.TextChanged)].IsBrowsable);
            Assert.False(events[nameof(YANTxt.StringChanged)].IsBrowsable);
            Assert.Equal(nameof(YANTxt.TextChanged), TypeDescriptor.GetDefaultEvent(t).Name);
            Assert.Equal(nameof(YANTxt.Text), TypeDescriptor.GetAttributes(t).OfType<DefaultBindingPropertyAttribute>().Single().Name);
        });

        // A [DefaultValue] that differs from the constructor makes the designer drop or add values
        [Theory]
        [InlineData(nameof(YANTxt.Text))]
        [InlineData(nameof(YANTxt.ReadOnly))]
        [InlineData(nameof(YANTxt.AcceptsReturn))]
        [InlineData(nameof(YANTxt.UseSystemPasswordChar))]
        public void DefaultValue_MatchesConstructor(string name) => Sta.Run(() =>
        {
            using var t = new YANTxt();
            var p = TypeDescriptor.GetProperties(t)[name];
            var dv = p.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
            Assert.NotNull(dv);
            Assert.Equal(dv.Value, p.GetValue(t));
            Assert.False(p.ShouldSerializeValue(t), name + " is serialized at its constructor value");
        });

        // ControlDesigner.InitializeNewComponent writes the site name into a browsable Text when a control is dropped (TextBox's
        // designer clears it again): a dropped YANTxt must start empty, while designer files being loaded keep their Text
        [Fact]
        public void DesignerDrop_DoesNotPutSiteNameIntoText() => Sta.Run(ui =>
        {
            var t = new YANTxt();
            var text = TypeDescriptor.GetProperties(t)[nameof(YANTxt.Text)];
            var changes = 0;
            t.TextChanged += (s, e) => changes++;
            t.Site = new DesignSite(t, "yanTxt1", false);
            Assert.True(text.IsBrowsable);
            // the designer may set it twice (InitializeNewComponent and the legacy OnSetComponentDefaults)
            text.SetValue(t, "yanTxt1");
            text.SetValue(t, "yanTxt1");
            Assert.Equal("", t.Text);
            Assert.Equal(0, changes);
            Assert.False(text.ShouldSerializeValue(t));
            // any other value is applied
            text.SetValue(t, "abc");
            Assert.Equal("abc", t.Text);
            text.SetValue(t, "");
            // once the control is on the design surface (painted), the site name typed in the property grid is an ordinary value
            ui.Draw(t);
            text.SetValue(t, "yanTxt1");
            Assert.Equal("yanTxt1", t.Text);
            t.Site = null;
            // loading a designer file: `this.yanTxt2.Text = "yanTxt2";` is kept
            using var loaded = new YANTxt();
            loaded.Site = new DesignSite(loaded, "yanTxt2", true);
            loaded.Text = "yanTxt2";
            Assert.Equal("yanTxt2", loaded.Text);
            loaded.Site = null;
            // at run time (no design-mode site) nothing is ignored
            using var runtime = new YANTxt { Name = "yanTxt3" };
            runtime.Text = "yanTxt3";
            Assert.Equal("yanTxt3", runtime.Text);
        });

        [Fact]
        public void ReadOnlyAndAcceptsReturn_PassThrough() => Sta.Run(() =>
        {
            using var t = new YANTxt();
            var inner = Inner(t);
            Assert.False(t.ReadOnly);
            Assert.False(t.AcceptsReturn);
            t.ReadOnly = true;
            Assert.True(inner.ReadOnly);
            // TextBox greys a read-only box unless its BackColor was set: the control's colors are kept
            Assert.Equal(Color.White, inner.BackColor);
            t.ReadOnly = false;
            Assert.False(inner.ReadOnly);
            t.AcceptsReturn = true;
            Assert.True(inner.AcceptsReturn);
            t.AcceptsReturn = false;
            Assert.False(inner.AcceptsReturn);
            // the clearer alias of PasswordChar
            t.UseSystemPasswordChar = true;
            Assert.True(t.PasswordChar);
            Assert.True(inner.UseSystemPasswordChar);
            t.PasswordChar = false;
            Assert.False(t.UseSystemPasswordChar);
        });

        // ENTER is suppressed only in a single-line box that does not accept returns (1.0.2 suppressed it even in multiline mode,
        // so no new line could be typed)
        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        public void Enter_SuppressedOnlyWhenSingleLineWithoutAcceptsReturn(bool multiline, bool acceptsReturn, bool suppressed) => Sta.Run(() =>
        {
            using var t = new YANTxt { Multiline = multiline, AcceptsReturn = acceptsReturn };
            var e = new KeyEventArgs(Keys.Enter);
            Priv.Call(t, "Txt_KeyDown", Inner(t), e);
            Assert.Equal(suppressed, e.SuppressKeyPress);
        });

        // Screen readers announce the inner text box, which receives the focus: it gets the accessible name of the control
        [Fact]
        public void AccessibleName_ForwardedToInnerBox() => Sta.Run(ui =>
        {
            var t = new YANTxt { AccessibleName = "User name", AccessibleDescription = "Login" };
            var inner = Inner(t);
            Assert.Null(inner.AccessibleName);
            // OnCreateControl runs when the control is created on a shown form
            ui.Show(t);
            Assert.Equal("User name", inner.AccessibleName);
            Assert.Equal("Login", inner.AccessibleDescription);
            // changed later: picked up when the box is entered, before it is announced
            t.AccessibleName = "E-mail";
            Priv.Call(t, "Txt_Enter", inner, EventArgs.Empty);
            Assert.Equal("E-mail", inner.AccessibleName);
            // changed later: also picked up when a screen reader asks the unfocused box for its accessible object (WM_GETOBJECT, OBJID_CLIENT)
            t.AccessibleName = "Login name";
            t.AccessibleDescription = "Account";
            Priv.Call(inner, "WndProc", Message.Create(inner.Handle, 0x003D, IntPtr.Zero, new IntPtr(-4)));
            Assert.Equal("Login name", inner.AccessibleName);
            Assert.Equal("Account", inner.AccessibleDescription);
            // a value set on the inner box directly is never overwritten
            inner.AccessibleDescription = "Own description";
            t.AccessibleName = "E-mail";
            t.AccessibleDescription = "Other";
            Priv.Call(t, "Txt_Enter", inner, EventArgs.Empty);
            Priv.Call(inner, "WndProc", Message.Create(inner.Handle, 0x003D, IntPtr.Zero, new IntPtr(-4)));
            Assert.Equal("Own description", inner.AccessibleDescription);
            Assert.Equal("E-mail", inner.AccessibleName);
            // set on the inner box before the control is created
            var t2 = new YANTxt { AccessibleName = "Outer" };
            Inner(t2).AccessibleName = "Inner";
            ui.Show(t2);
            Assert.Equal("Inner", Inner(t2).AccessibleName);
        });

        #region 2.0 painting (the edit-box painting is shared by YANTxt and YANNb)
        // The rounded corners show what the parent paints (lime), not a ring of its BackColor (white); the border and the surface keep
        // their colors (1.x: a Parent.BackColor ring, #15)
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void RoundedCorners_ShowTheParent_BorderAndSurfaceKeepTheirColors(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 15, 2);
            var pnl = OnPanel(ui, new PaintingTests.LimePanel(), c);
            using var bmp = ui.Render(c);
            foreach (var corner in EditCorners(c))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(corner.X, corner.Y), $"{type.Name} corner {corner}");
            }
            PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, 1), "top border");
            PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, c.Height - 1), "bottom border");
            PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, 4), "surface above the inner box");
            PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(5, c.Height / 2), "surface left of the inner box");
            Assert.Equal(Color.White, pnl.BackColor);
            Assert.Null(c.Region);
        });

        // On a gradient, every corner shows the gradient pixel behind it (1.x: a solid ring in the panel's BackColor, #15)
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void RoundedCorners_ShowTheGradientBehind(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 15, 2);
            var pnl = OnPanel(ui, new YANGradPnl { TopColor = Color.Red, BottomColor = Color.Blue, BackColor = Color.White, Size = new Size(300, 200) }, c);
            // created (and fitted to its font) before the parent is captured without it
            c.CreateControl();
            c.Visible = false;
            using var bmpParent = ui.Render(pnl);
            c.Visible = true;
            using var bmp = ui.Render(c);
            foreach (var corner in EditCorners(c))
            {
                var behind = bmpParent.GetPixel(c.Left + corner.X, c.Top + corner.Y);
                Assert.False(Gdi.Same(behind, Color.White), "the gradient must differ from the panel's BackColor");
                PaintingTests.AssertColor(behind, bmp.GetPixel(corner.X, corner.Y), $"{type.Name} corner {corner}", 3);
            }
        });

        // The rounded edge is anti-aliased (a region clips at whole pixels): the corner has pixels blended between the parent and the border
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void RoundedCorners_AreAntiAliased(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 15, 2);
            OnPanel(ui, new PaintingTests.LimePanel(), c);
            using var bmp = ui.Render(c);
            var blended = 0;
            for (var y = 0; y < 15; y++)
            {
                for (var x = 0; x < 15; x++)
                {
                    var px = bmp.GetPixel(x, y);
                    if (!Gdi.Same(px, Color.Lime) && !Gdi.Same(px, Color.Red) && !Gdi.Same(px, Color.Blue))
                    {
                        blended++;
                    }
                }
            }
            Assert.True(blended > 0, "no anti-aliased pixel in the rounded corner");
        });

        // An opaque square box covers its whole area: the parent is not painted behind it (its Paint handlers do not run again);
        // rounded corners paint it
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void ParentPainted_OnlyBehindRoundedCorners(Type type) => Sta.Run(ui =>
        {
            var pnl = new PaintingTests.LimePanel();
            var paints = 0;
            pnl.Paint += (s, e) => paints++;
            var c = Edit(type, 0, 2);
            OnPanel(ui, pnl, c);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(1, 1), "square corner: border");
            }
            Assert.Equal(0, paints);
            SetRadius(c, 15);
            using (ui.Render(c))
            {
            }
            Assert.True(paints > 0, "the parent is not painted behind the rounded corners");
        });

        // The underline is cut by the rounded corners (1.x: by the region) and is as thick as 1.x drew it, the part inside the box of a
        // BorderSize-wide pen centred on the bottom row: BorderSize / 2 + 1 rows (one for 0, except on a rounded YANNb, where 1.x drew none)
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void Underline_CutByTheRoundedCorners_AsThickAsIn1x(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 15, 2, true);
            OnPanel(ui, new PaintingTests.LimePanel(), c);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, c.Height - 1), "underline");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, c.Height - 4), "above the underline of 2");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, 1), "no top border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(1, c.Height / 2), "no left border");
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(0, c.Height - 1), "bottom-left corner");
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(c.Width - 1, c.Height - 1), "bottom-right corner");
            }
            SetBorderSize(c, 4);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, c.Height - 3), "third row of the underline of 4");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, c.Height - 4), "above the underline of 4");
            }
            SetBorderSize(c, 0);
            using (var bmp = ui.Render(c))
            {
                if (c is YANNb)
                {
                    PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, c.Height - 1), "no underline for BorderSize 0 on a rounded YANNb");
                }
                else
                {
                    PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, c.Height - 1), "one-pixel underline for BorderSize 0");
                }
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, c.Height - 3), "above the one-pixel underline");
            }
            // square corners: a one-pixel underline for BorderSize 0 on both, as in 1.x
            SetRadius(c, 0);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, c.Height - 1), "one-pixel underline for BorderSize 0, square");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, c.Height - 2), "above the one-pixel underline, square");
            }
            SetRadius(c, 15);
            // the box style draws nothing for BorderSize 0
            SetUnderline(c, false);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, c.Height - 1), "no border for BorderSize 0");
            }
        });

        // A BackgroundImage is still painted (as the default background painted it in 1.x), inside the rounded shape
        [Theory]
        [InlineData(typeof(YANTxt), 0)]
        [InlineData(typeof(YANTxt), 15)]
        [InlineData(typeof(YANNb), 0)]
        [InlineData(typeof(YANNb), 15)]
        public void BackgroundImage_IsPaintedInTheShape(Type type, int radius) => Sta.Run(ui =>
        {
            var c = Edit(type, radius, 2);
            using var img = new Bitmap(8, 8);
            using (var g = Graphics.FromImage(img))
            {
                g.Clear(Color.Yellow);
            }
            c.BackgroundImage = img;
            OnPanel(ui, new PaintingTests.LimePanel(), c);
            using var bmp = ui.Render(c);
            PaintingTests.AssertColor(Color.Yellow, bmp.GetPixel(5, c.Height / 2), "image left of the inner box");
            PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, 1), "border over the image");
            PaintingTests.AssertColor(radius > 0 ? Color.Lime : Color.Red, bmp.GetPixel(1, 1), "corner");
        });

        // BorderSize and BorderRadius are 96-dpi pixels: at 192 dpi a border of 2 is 4 pixels wide (a DPI-unaware application stays at 96)
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void Dpi_ScalesTheBorder(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 0, 2);
            OnPanel(ui, new PaintingTests.LimePanel(), c);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, 1), "96 dpi: second row");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, 3), "96 dpi: fourth row");
            }
            YANPaint.DpiOverride = 192;
            try
            {
                using var bmp = ui.Render(c);
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, 1), "192 dpi: second row");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(c.Width / 2, 3), "192 dpi: fourth row");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(3, c.Height / 2), "192 dpi: fourth column");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, 5), "192 dpi: sixth row");
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // High contrast mode paints the border with the system frame color, and the highlight color while the box has the focus
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void HighContrast_BorderUsesSystemColors(Type type) => Sta.Run(ui =>
        {
            YANPaint.HighContrastOverride = true;
            try
            {
                var c = Edit(type, 0, 2);
                OnPanel(ui, new PaintingTests.LimePanel(), c);
                using (var bmp = ui.Render(c))
                {
                    PaintingTests.AssertColor(SystemColors.WindowFrame, bmp.GetPixel(c.Width / 2, 1), "border");
                }
                EnterEdit(c);
                using (var bmp = ui.Render(c))
                {
                    PaintingTests.AssertColor(SystemColors.Highlight, bmp.GetPixel(c.Width / 2, 1), "focused border");
                }
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
        });

        // Zero, tiny and huge sizes, radii and borders on a painted parent (1000 x 1000 at most: a rounded box clips its inner editor
        // with a region, and libgdiplus crashes on a region wider than about 1000 x 2000 pixels; GDI+ has no such limit)
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void TinySizes_OnAPaintedParent_Draw(Type type) => Sta.Run(ui =>
        {
            foreach (var size in new[] { new Size(0, 0), new Size(1, 1), new Size(3, 1), new Size(1, 3), new Size(1000, 1000) })
            {
                foreach (var (radius, border) in new[] { (0, 0), (15, 2), (1000, 1000), (0, 49), (int.MaxValue, int.MaxValue) })
                {
                    foreach (var underline in new[] { false, true })
                    {
                        ui.Case($"{type.Name} {size.Width}x{size.Height} r={radius} b={border} underline={underline}", () =>
                        {
                            var c = Sweep.Sized(Edit(type, radius, border, underline), size);
                            OnPanel(ui, new PaintingTests.LimePanel(), c);
                            ui.DrawAndDispose(c);
                        });
                    }
                }
            }
        });

        // Moving the box to another parent shows the new parent in its corners; a new window handle changes nothing
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void ParentChange_AndHandleRecreation(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 15, 2);
            OnPanel(ui, new PaintingTests.LimePanel(), c);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(1, 1), "first parent");
            }
            var pnlRed = OnPanel(ui, new PaintingTests.ColorPanel(Color.Red), c);
            Assert.Same(pnlRed, c.Parent);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(1, 1), "second parent");
            }
            var handle = c.Handle;
            Priv.Call(c, "RecreateHandle");
            Assert.NotEqual(handle, c.Handle);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(1, 1), "after RecreateHandle");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(5, c.Height / 2), "surface after RecreateHandle");
            }
            pnlRed.Controls.Remove(c);
            ui.DrawDetached(c);
            c.Dispose();
        });

        // A padding too small for the rounded corners: the inner editor is clipped by the rounded shape of the box, as the 1.x region
        // of the box clipped it (the box itself has no region); with the default padding it is inside the shape and has no region
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void SmallPadding_ClipsTheInnerEditorByTheRoundedShape(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 12, 2);
            var inner = EditInner(c);
            ui.Draw(c);
            Assert.Null(inner.Region);
            c.Padding = new Padding(2);
            var region = inner.Region;
            Assert.NotNull(region);
            Assert.Null(c.Region);
            using (var g = c.CreateGraphics())
            {
                // the corner of the editor lies outside the rounded corner of the box; its middle is kept
                Assert.False(region.IsVisible(0, 0, g), "the corner of the inner editor is not clipped");
                Assert.True(region.IsVisible(inner.Width / 2, inner.Height / 2, g), "the middle of the inner editor is clipped");
            }
            ui.Draw(c);
            Assert.Same(region, inner.Region);
            c.Padding = new Padding(10, 7, 10, 7);
            Assert.Null(inner.Region);
            SetRadius(c, 0);
            c.Padding = new Padding(0);
            Assert.Null(inner.Region);
        });

        // The transparent corners let clicks through to the parent, as the rounded region of 1.x did; the rest of the box takes them
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void HitTest_RoundedCornersAreTransparent(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 15, 2);
            OnPanel(ui, new PaintingTests.LimePanel(), c);
            ui.Draw(c);
            Assert.Equal(HTTRANSPARENT, HitTest(c, new Point(1, 1)));
            Assert.Equal(HTTRANSPARENT, HitTest(c, new Point(c.Width - 2, c.Height - 2)));
            Assert.NotEqual(HTTRANSPARENT, HitTest(c, new Point(5, c.Height / 2)));
            Assert.NotEqual(HTTRANSPARENT, HitTest(c, new Point(c.Width / 2, 2)));
            SetRadius(c, 0);
            Assert.NotEqual(HTTRANSPARENT, HitTest(c, new Point(1, 1)));
        });

        // 2.0: the box scales with its form (1.x forced AutoScaleMode.None on it); after the form's AutoScale, the height of the
        // single-line box still fits its font and its padding
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void AutoScale_FollowsTheForm_HeightFitsTheFont(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 0, 2);
            Assert.Equal(AutoScaleMode.Inherit, c.AutoScaleMode);
            using var frm = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = Point.Empty, Size = new Size(700, 300) };
            frm.SuspendLayout();
            // designed at 64 dpi, shown at the screen's DPI (96 or more): the form scales its controls by 1.5 or more
            frm.AutoScaleDimensions = new SizeF(64F, 64F);
            frm.AutoScaleMode = AutoScaleMode.Dpi;
            c.Location = new Point(10, 10);
            frm.Controls.Add(c);
            frm.ResumeLayout(false);
            frm.PerformAutoScale();
            Assert.True(c.Width > 200, $"not scaled with the form (width {c.Width})");
            frm.Show();
            ui.Pump(2);
            var inner = EditInner(c);
            Assert.Equal(inner.Height + c.Padding.Vertical, c.Height);
            Assert.True(inner.Top >= c.Padding.Top && inner.Bottom <= c.Height - c.Padding.Bottom, $"inner box {inner.Bounds} outside the padding {c.Padding} of {c.Size}");
        });

        // Moved to a monitor with another DPI (per-monitor aware host): once the form has rescaled everything (WM_DPICHANGED_AFTERPARENT),
        // the height of the single-line box fits its (rescaled) font again, without a new window for the inner editor
        [Theory]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        public void DpiChangedAfterParent_FitsTheHeightToTheFontAgain(Type type) => Sta.Run(ui =>
        {
            var c = Edit(type, 0, 2);
            ui.Show(c);
            var inner = EditInner(c);
            var fitted = c.Height;
            Assert.Equal(inner.Height + c.Padding.Vertical, fitted);
            c.Font = new Font(c.Font.FontFamily, c.Font.Size * 2);
            // a run-time font change alone leaves the height to the application, as in 1.x
            Assert.Equal(fitted, c.Height);
            // the user may be typing in the box: its window (undo history, scroll position, caret) is kept, so the text box is not made
            // multiline for a moment (on .NET that recreates its window) as when the form loads
            var handle = inner.Handle;
            var multilineChanges = 0;
            if (inner is TextBox txt)
            {
                txt.MultilineChanged += (s, e) => multilineChanges++;
            }
            Send(c, WM_DPICHANGED_AFTERPARENT);
            Assert.True(c.Height > fitted, "the height did not follow the bigger font");
            Assert.Equal(inner.Height + c.Padding.Vertical, c.Height);
            Assert.Equal(handle, inner.Handle);
            Assert.Equal(0, multilineChanges);
            // other messages change nothing
            c.Height = 100;
            Send(c, 0x0400);
            Assert.Equal(100, c.Height);
        });
        #endregion

        #region Helpers
        private const int HTTRANSPARENT = -1;
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_DPICHANGED_AFTERPARENT = 0x02E3;

        // The inner text box (a private CueTextBox)
        private static TextBox Inner(YANTxt t) => Priv.Field<TextBox>(t, "_txtText");

        // A 200 x 40 blue edit box (YANTxt or YANNb) with a red border
        internal static UserControl Edit(Type type, int radius, int border, bool underline = false)
        {
            UserControl c = type == typeof(YANNb)
                ? new YANNb { BorderRadius = radius, BorderSize = border, UnderlinedStyle = underline, BorderColor = Color.Red, BackColor = Color.Blue }
                : new YANTxt { BorderRadius = radius, BorderSize = border, UnderlinedStyle = underline, BorderColor = Color.Red, BackColor = Color.Blue };
            c.Size = new Size(200, 40);
            return c;
        }

        // The inner editor of an edit box
        internal static System.Windows.Forms.Control EditInner(UserControl c) => c is YANNb ? Priv.Field<NumericUpDown>(c, "_nudNum") : Inner((YANTxt)c);

        // Puts the edit box on a panel of the hidden host form, out of the panel's corner
        internal static T OnPanel<T>(Ui ui, T pnl, UserControl c) where T : System.Windows.Forms.Control
        {
            if (pnl.Parent == null)
            {
                ui.Host.Controls.Add(pnl);
            }
            c.Location = new Point(30, 20);
            pnl.Controls.Add(c);
            return pnl;
        }

        // Pixels of the four corners that a radius of 15 leaves to the parent
        private static Point[] EditCorners(System.Windows.Forms.Control c) => new[] { new Point(1, 1), new Point(c.Width - 2, 1), new Point(1, c.Height - 2), new Point(c.Width - 2, c.Height - 2) };

        private static void SetRadius(UserControl c, int radius)
        {
            if (c is YANNb n)
            {
                n.BorderRadius = radius;
            }
            else
            {
                ((YANTxt)c).BorderRadius = radius;
            }
        }

        private static void SetBorderSize(UserControl c, int size)
        {
            if (c is YANNb n)
            {
                n.BorderSize = size;
            }
            else
            {
                ((YANTxt)c).BorderSize = size;
            }
        }

        private static void SetUnderline(UserControl c, bool underline)
        {
            if (c is YANNb n)
            {
                n.UnderlinedStyle = underline;
            }
            else
            {
                ((YANTxt)c).UnderlinedStyle = underline;
            }
        }

        // Runs the Enter handler of the inner editor (the box then paints its focused border)
        private static void EnterEdit(UserControl c) => Priv.Call(c, c is YANNb ? "Nud_Enter" : "Txt_Enter", EditInner(c), EventArgs.Empty);

        // WM_NCHITTEST at a client point, through the window procedure
        private static int HitTest(UserControl c, Point client)
        {
            var screen = c.PointToScreen(client);
            var args = new object[] { Message.Create(c.Handle, WM_NCHITTEST, IntPtr.Zero, (IntPtr)unchecked((screen.Y << 16) | (screen.X & 0xFFFF))) };
            Priv.Call(c, "WndProc", args);
            return (int)((Message)args[0]).Result.ToInt64();
        }

        // A message without parameters, through the window procedure
        private static void Send(UserControl c, int msg) => Priv.Call(c, "WndProc", new object[] { Message.Create(c.Handle, msg, IntPtr.Zero, IntPtr.Zero) });

        // Moves the focus into the box; runs the Enter handler itself where the window could not take the focus
        private static void Focus(Ui ui, YANTxt t, TextBox inner)
        {
            inner.Focus();
            ui.Pump(2);
            if (!inner.Focused)
            {
                Priv.Call(t, "Txt_Enter", inner, EventArgs.Empty);
            }
        }

        // Moves the focus to another control; runs the Leave handler itself where the focus did not move
        private static void Blur(Ui ui, YANTxt t, TextBox inner, System.Windows.Forms.Control other)
        {
            other.Focus();
            ui.Pump(2);
            if (inner.Focused || t.String != null)
            {
                Priv.Call(t, "Txt_Leave", inner, EventArgs.Empty);
            }
        }

        private static int Red(TextBox inner) => Gdi.CountOnScreen(inner, Gdi.IsRed);

        private static int Dark(TextBox inner) => Gdi.CountOnScreen(inner, Gdi.IsDarkGray);
        #endregion

        // Counts the calls of the protected virtual OnTextChanged
        private sealed class TxtProbe : YANTxt
        {
            public int Calls { get; private set; }

            protected override void OnTextChanged(EventArgs e)
            {
                Calls++;
                base.OnTextChanged(e);
            }
        }

        // The design-time site of a control dropped on (or loaded into) a designer; it is its own IDesignerHost for Loading
        private sealed class DesignSite : ISite, IDesignerHost
        {
            public DesignSite(IComponent component, string name, bool loading)
            {
                Component = component;
                Name = name;
                Loading = loading;
            }

            public IComponent Component { get; }

            public IContainer Container => null;

            public bool DesignMode => true;

            public string Name { get; set; }

            public bool Loading { get; }

            public object GetService(Type serviceType) => serviceType == typeof(IDesignerHost) ? this : null;

            // The rest of IDesignerHost is not used by the control
            bool IDesignerHost.InTransaction => false;

            IComponent IDesignerHost.RootComponent => null;

            string IDesignerHost.RootComponentClassName => null;

            string IDesignerHost.TransactionDescription => null;

            event EventHandler IDesignerHost.Activated { add { } remove { } }

            event EventHandler IDesignerHost.Deactivated { add { } remove { } }

            event EventHandler IDesignerHost.LoadComplete { add { } remove { } }

            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosed { add { } remove { } }

            event DesignerTransactionCloseEventHandler IDesignerHost.TransactionClosing { add { } remove { } }

            event EventHandler IDesignerHost.TransactionOpened { add { } remove { } }

            event EventHandler IDesignerHost.TransactionOpening { add { } remove { } }

            void IDesignerHost.Activate() => throw new NotSupportedException();

            IComponent IDesignerHost.CreateComponent(Type componentClass) => throw new NotSupportedException();

            IComponent IDesignerHost.CreateComponent(Type componentClass, string name) => throw new NotSupportedException();

            DesignerTransaction IDesignerHost.CreateTransaction() => throw new NotSupportedException();

            DesignerTransaction IDesignerHost.CreateTransaction(string description) => throw new NotSupportedException();

            void IDesignerHost.DestroyComponent(IComponent component) => throw new NotSupportedException();

            IDesigner IDesignerHost.GetDesigner(IComponent component) => null;

            Type IDesignerHost.GetType(string typeName) => Type.GetType(typeName);

            void IServiceContainer.AddService(Type serviceType, object serviceInstance) => throw new NotSupportedException();

            void IServiceContainer.AddService(Type serviceType, object serviceInstance, bool promote) => throw new NotSupportedException();

            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback) => throw new NotSupportedException();

            void IServiceContainer.AddService(Type serviceType, ServiceCreatorCallback callback, bool promote) => throw new NotSupportedException();

            void IServiceContainer.RemoveService(Type serviceType) => throw new NotSupportedException();

            void IServiceContainer.RemoveService(Type serviceType, bool promote) => throw new NotSupportedException();
        }
    }
}
