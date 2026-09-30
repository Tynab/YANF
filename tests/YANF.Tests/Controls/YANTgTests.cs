using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANTgTests
    {
        [Fact]
        public void AllSizesAndStates_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes.Concat(new[] { new Size(45, 22), new Size(22, 45), new Size(2, 2) }))
            {
                foreach (var on in new[] { false, true })
                {
                    foreach (var solid in new[] { false, true })
                    {
                        ui.Case($"YANTg {size.Width}x{size.Height} on={on} solid={solid}", () => ui.DrawAndDispose(Sweep.Sized(new YANTg { Checked = on, SolidStyle = solid }, size)));
                    }
                }
            }
        });

        [Fact]
        public void NoParent_Draws() => Sta.Run(ui =>
        {
            using var t = new YANTg { Checked = true };
            ui.DrawDetached(t);
            t.Checked = false;
            ui.DrawDetached(t);
        });

        // 1.0 forced TabStop = false: TAB now reaches a new toggle (1.0 designer files keep their explicit TabStop = false)
        [Fact]
        public void Tab_ReachesTheToggle() => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var legacy = new YANTg { TabStop = false };
            var t = new YANTg();
            var frm = ui.Show(txt, legacy, t);
            Assert.True(t.TabStop);
            Assert.True(frm.SelectNextControl(txt, true, true, true, true));
            Assert.Same(t, frm.ActiveControl);
        });

        // SPACE toggles the focused toggle (ButtonBase's key handling, then CheckBox.OnClick)
        [Fact]
        public void Space_Toggles() => Sta.Run(ui =>
        {
            var t = new YANTg();
            var changes = 0;
            t.CheckedChanged += (s, e) => changes++;
            ui.Show(t);
            PressSpace(t);
            Assert.True(t.Checked);
            PressSpace(t);
            Assert.False(t.Checked);
            Assert.Equal(2, changes);
            // AutoCheck = false leaves the state to the application, as for a CheckBox
            t.AutoCheck = false;
            PressSpace(t);
            Assert.False(t.Checked);
        });

        [Fact]
        public void FocusCue_AllSizesAndStates_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes.Concat(new[] { new Size(45, 22), new Size(22, 45), new Size(2, 2), new Size(5, 4), new Size(6, 5) }))
            {
                foreach (var on in new[] { false, true })
                {
                    foreach (var solid in new[] { false, true })
                    {
                        ui.Case($"focused YANTg {size.Width}x{size.Height} on={on} solid={solid}", () => ui.DrawAndDispose(Sweep.Sized(new AllControlsTests.FocusedTg { Checked = on, SolidStyle = solid }, size)));
                    }
                }
            }
        });

        // The cue is drawn in the toggle color in both styles, on the row just inside the edge of the surface: it contrasts with a
        // solid surface, and on an outlined one it stands out from the outline it runs along (in the outline color it would only
        // make the outline look thicker)
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FocusCue_InTheToggleColor_InsideTheEdge(bool solid) => Sta.Run(ui =>
        {
            Bitmap Render(YANTg t)
            {
                t.SolidStyle = solid;
                t.Size = new Size(90, 40);
                t.OffBackColor = Color.Blue;
                t.OffToggleColor = Color.Lime;
                return ui.Render(t);
            }
            // the straight part of the top edge (the ends are half circles of diameter 39), away from the toggle on the left
            var straight = new Rectangle(45, 0, 20, 1);
            using (var bmp = Render(new YANTg()))
            {
                Assert.Equal(0, CountLime(bmp, straight));
                Assert.Equal(0, CountLime(bmp, Row(straight, 1)));
            }
            using (var bmp = Render(new AllControlsTests.FocusedTg()))
            {
                // row 0 is the edge (the outline, or the edge of the solid surface): the cue stays inside it, on row 1
                Assert.Equal(0, CountLime(bmp, straight));
                Assert.True(CountLime(bmp, Row(straight, 1)) >= straight.Width / 3, "focus cue not drawn in the toggle color on row 1");
            }
        });

        // Text is still not stored (D4 item 4 was not needed): AutoSize keeps the toggle at its MinimumSize whatever the Text
        [Fact]
        public void Text_NotStored_AutoSizeIgnoresIt() => Sta.Run(ui =>
        {
            var t = new YANTg { AutoSize = true, Text = "a rather long label" };
            Assert.Equal("", t.Text);
            ui.Host.Controls.Add(t);
            ui.Host.PerformLayout();
            Assert.Equal(new Size(45, 22), t.Size);
        });

        private static Rectangle Row(Rectangle rect, int y) => new(rect.X, y, rect.Width, 1);

        // Counts the lime pixels of a part of the bitmap
        private static int CountLime(Bitmap bmp, Rectangle rect)
        {
            var n = 0;
            for (var x = rect.Left; x < rect.Right; x++)
            {
                for (var y = rect.Top; y < rect.Bottom; y++)
                {
                    if (Gdi.IsLime(bmp.GetPixel(x, y)))
                    {
                        n++;
                    }
                }
            }
            return n;
        }

        private static void PressSpace(YANTg t)
        {
            Priv.Call(t, "OnKeyDown", new KeyEventArgs(Keys.Space));
            Priv.Call(t, "OnKeyUp", new KeyEventArgs(Keys.Space));
        }
    }
}
