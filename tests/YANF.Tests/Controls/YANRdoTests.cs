using System;
using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANRdoTests
    {
        // The text starts right of the 18 px circle and its 8 px gap
        private const int TEXT_X = 26;

        [Fact]
        public void AllSizesAndStates_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var on in new[] { false, true })
                {
                    ui.Case($"YANRdo {size.Width}x{size.Height} on={on}", () => ui.DrawAndDispose(Sweep.Sized(new YANRdo { Text = "radio", Checked = on }, size)));
                }
            }
        });

        // 1.0.2 swapped ForeColor on hover, so a ForeColor set while hovering was lost on MouseLeave
        [Fact]
        public void Hover_KeepsForeColor() => Sta.Run(ui =>
        {
            var r = new YANRdo { Text = "radio", ForeColor = Color.Green, HighlightText = Color.Red };
            var foreChanges = 0;
            r.ForeColorChanged += (s, e) => foreChanges++;
            Priv.Call(r, "OnMouseEnter", EventArgs.Empty);
            Assert.Equal(Color.Green, r.ForeColor);
            ui.Draw(r);
            r.ForeColor = Color.Blue;
            Priv.Call(r, "OnMouseLeave", EventArgs.Empty);
            Assert.Equal(Color.Blue, r.ForeColor);
            Assert.Equal(1, foreChanges);
            ui.Draw(r);
        });

        // Hover still paints the text in HighlightText; only the ForeColor property stays untouched
        [Fact]
        public void Hover_PaintsHighlightText() => Sta.Run(ui =>
        {
            // a big bold font, so that whatever the text anti-aliasing, the strokes have fully colored pixels
            var r = new YANRdo
            {
                Text = "WWWW",
                Font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold),
                ForeColor = Color.Lime,
                HighlightText = Color.Red,
                BackColor = Color.White,
                Size = new Size(200, 40)
            };
            AssertText(ui, r, lime: true, "not hovered");
            Priv.Call(r, "OnMouseEnter", EventArgs.Empty);
            AssertText(ui, r, lime: false, "hovered");
            Priv.Call(r, "OnMouseLeave", EventArgs.Empty);
            AssertText(ui, r, lime: true, "after leave");
            Assert.Equal(Color.Lime, r.ForeColor);
        });

        // RadioButton's own TabStop rules apply (1.0 also forced false, the RadioButton default): the checked radio of a group
        // is the one TAB reaches
        [Fact]
        public void TabStop_FollowsRadioButtonRules() => Sta.Run(ui =>
        {
            var r = new YANRdo { Text = "radio" };
            Assert.Equal(new RadioButton().TabStop, r.TabStop);
            var txt = new TextBox();
            var frm = ui.Show(txt, r);
            r.Checked = true;
            Assert.True(r.TabStop);
            Assert.True(frm.SelectNextControl(txt, true, true, true, true));
            Assert.Same(r, frm.ActiveControl);
        });

        // SPACE checks the focused radio button (ButtonBase's key handling, then RadioButton.OnClick); PerformClick too
        [Fact]
        public void SpaceAndPerformClick_Check() => Sta.Run(ui =>
        {
            var r1 = new YANRdo { Text = "one" };
            var r2 = new YANRdo { Text = "two" };
            ui.Show(r1, r2);
            Priv.Call(r1, "OnKeyDown", new KeyEventArgs(Keys.Space));
            Priv.Call(r1, "OnKeyUp", new KeyEventArgs(Keys.Space));
            Assert.True(r1.Checked);
            r2.PerformClick();
            Assert.True(r2.Checked);
            Assert.False(r1.Checked);
        });

        // The text is drawn with TextRenderer, which measures it: it starts right of the circle and stays within its measured width
        [Fact]
        public void Text_DrawnWithinItsMeasuredWidth() => Sta.Run(ui =>
        {
            var r = Big(new YANRdo { Text = "WWWW" });
            using var bmp = ui.Render(r);
            var (first, last) = LimeColumns(bmp);
            var width = TextRenderer.MeasureText(r.Text, r.Font).Width;
            Assert.True(first >= TEXT_X && first < TEXT_X + width, $"text starts at x={first}");
            Assert.True(last < TEXT_X + width, $"text ends at x={last}, measured end {TEXT_X + width}");
        });

        // '&' is drawn as it is, as 1.0 drew it with Graphics.DrawString, whatever UseMnemonic: switching to TextRenderer must not
        // change the text of existing forms (the RadioButton's own mnemonic handling is unchanged)
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ampersand_DrawnAsIn10(bool useMnemonic) => Sta.Run(ui =>
        {
            int LastLime(string text)
            {
                using var bmp = ui.Render(Big(new YANRdo { Text = text, UseMnemonic = useMnemonic }));
                return LimeColumns(bmp).Last;
            }
            Assert.True(LastLime("&WWWW") > LastLime("WWWW"), "the '&' must be drawn");
            Assert.True(LastLime("W&&W") > LastLime("W&W"), "'&&' must be drawn as two ampersands");
        });

        [Fact]
        public void FocusCue_AllSizesAndStates_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var text in new[] { "", "radio" })
                {
                    ui.Case($"focused YANRdo {size.Width}x{size.Height} text='{text}'", () => ui.DrawAndDispose(Sweep.Sized(new AllControlsTests.FocusedRdo { Text = text }, size)));
                }
            }
        });

        // A big bold lime text on white, so that the strokes have fully colored pixels whatever the anti-aliasing
        private static YANRdo Big(YANRdo r)
        {
            r.Font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold);
            r.ForeColor = Color.Lime;
            r.BackColor = Color.White;
            r.Size = new Size(300, 40);
            return r;
        }

        // First and last column with a lime pixel
        private static (int First, int Last) LimeColumns(Bitmap bmp)
        {
            var (first, last) = (-1, -1);
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    if (Gdi.IsLime(bmp.GetPixel(x, y)))
                    {
                        first = first < 0 ? x : first;
                        last = x;
                        break;
                    }
                }
            }
            Assert.True(first >= 0, "no text drawn");
            return (first, last);
        }

        // Lime text = ForeColor, red text = HighlightText
        private static void AssertText(Ui ui, YANRdo r, bool lime, string when)
        {
            using var bmp = ui.Render(r);
            var red = Gdi.Count(bmp, Gdi.IsRed, TEXT_X);
            var green = Gdi.Count(bmp, Gdi.IsLime, TEXT_X);
            Assert.True(lime ? green > 0 && red == 0 : red > 0 && green == 0, $"{when}: text must be {(lime ? "ForeColor (lime)" : "HighlightText (red)")}, counted lime={green} red={red}");
        }
    }
}
