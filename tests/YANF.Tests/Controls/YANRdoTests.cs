using System;
using System.Drawing;
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
