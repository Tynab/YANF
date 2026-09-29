using System;
using System.Drawing;
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
            t.StringChanged += (s, e) => changes++;
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
            Assert.Equal(0, changes);
            Assert.Equal("", inner.Text);
            Assert.Null(t.String);
            inner.Focus();
            ui.Pump(2);
            t.String = "abc";
            other.Focus();
            ui.Pump(2);
            Assert.Equal(1, changes);
            Assert.Equal("abc", t.String);
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

        // 1.0.2 suppressed Enter even in multiline mode, so no new line could be typed
        [Fact]
        public void Enter_SuppressedOnlyWhenSingleLine() => Sta.Run(() =>
        {
            using var t = new YANTxt();
            var e = new KeyEventArgs(Keys.Enter);
            Priv.Call(t, "Txt_KeyDown", Inner(t), e);
            Assert.True(e.SuppressKeyPress, "single line suppresses Enter");
            t.Multiline = true;
            e = new KeyEventArgs(Keys.Enter);
            Priv.Call(t, "Txt_KeyDown", Inner(t), e);
            Assert.False(e.SuppressKeyPress, "multiline lets Enter insert a new line");
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
    }
}
