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

        // 1.0.2 assigned a new Region in OnPaint (repaint loop); regions now change with the shape only
        [Fact]
        public void Region_NotRebuiltByPaint() => Sta.Run(ui =>
        {
            var t = new YANTxt { Size = new Size(200, 40), BorderRadius = 20, BorderSize = 2 };
            ui.Draw(t);
            var region = t.Region;
            Assert.NotNull(region);
            Assert.NotNull(Inner(t).Region);
            ui.Draw(t);
            Assert.Same(region, t.Region);
            t.BorderRadius = 0;
            Assert.Null(t.Region);
            Assert.Null(Inner(t).Region);
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

        // The inner text box (a private CueTextBox)
        private static TextBox Inner(YANTxt t) => Priv.Field<TextBox>(t, "_txtText");

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
