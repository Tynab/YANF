using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Xunit;
using YANF.Control;
using YANF.Script;

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
        // make the outline look thicker). Its dots are whole pixels: anti-aliased, a dot that does not start on a pixel edge is
        // spread over two half-colored pixels (on Windows the line then looked blurred, with no pixel in the toggle color)
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
                Assert.Equal(0, Count(bmp, Row(straight, 1), AllControlsTests.IsPartlyLime));
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

        // Without Windows animation effects (reduced motion) the toggle jumps to its new position
        [Fact]
        public void Checked_Jumps_WithoutAnimationEffects() => Sta.Run(ui => WithEffects(false, () =>
        {
            var t = new YANTg();
            ui.Show(t);
            t.Checked = true;
            Assert.False(IsSliding(t));
            Assert.Equal(1f, Knob(t));
            t.Checked = false;
            Assert.False(IsSliding(t));
            Assert.Equal(0f, Knob(t));
        }));

        // It also jumps while it cannot be seen: no window yet (InitializeComponent), hidden, or in the designer
        [Fact]
        public void Checked_Jumps_WhenItCannotBeSeen() => Sta.Run(ui => WithEffects(true, () =>
        {
            using (var t = new YANTg())
            {
                t.Checked = true;
                Assert.False(IsSliding(t));
                Assert.Equal(1f, Knob(t));
            }
            var hidden = new YANTg();
            ui.Show(hidden);
            hidden.Visible = false;
            hidden.Checked = true;
            Assert.False(IsSliding(hidden));
            Assert.Equal(1f, Knob(hidden));
            var designed = new YANTg
            {
                Site = new DesignSite()
            };
            ui.Show(designed);
            designed.Checked = true;
            Assert.False(IsSliding(designed));
            Assert.Equal(1f, Knob(designed));
        }));

        // A toggle set while its form loads (Form_Load restoring the settings) is shown at its new position when the form appears: its
        // window exists and Visible is already true, but nothing is on screen yet. Once painted, it slides
        [Fact]
        public void Checked_Jumps_WhileTheFormLoads() => Sta.Run(ui => WithEffects(true, () =>
        {
            var t = new YANTg();
            using var frm = new Form
            {
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = Point.Empty
            };
            frm.Controls.Add(t);
            bool? slidingInLoad = null;
            var isShownInLoad = false;
            frm.Load += (s, e) =>
            {
                // what 1.x-style checks see: the window exists and the toggle is Visible
                isShownInLoad = t.IsHandleCreated && t.Visible;
                t.Checked = true;
                slidingInLoad = IsSliding(t);
            };
            frm.Show();
            Assert.True(isShownInLoad, "the scenario changed: no window or not Visible during Load");
            Assert.False(slidingInLoad ?? true, slidingInLoad == null ? "Load was not raised" : "the toggle slid while its form was loading");
            Assert.Equal(1f, Knob(t));
            ui.Pump();
            t.Checked = false;
            Assert.True(IsSliding(t), "no slide once the form is on screen");
            Assert.True(EndSlide(ui, t), "the slide did not end");
            // hidden, then shown again: nothing slides before the next paint
            frm.Hide();
            frm.Show();
            t.Checked = true;
            Assert.False(IsSliding(t));
            Assert.Equal(1f, Knob(t));
        }));

        // With animation effects the toggle slides from where it is drawn (about 150 ms, ease-out), then rests at the new position
        [Fact]
        public void Checked_Slides_ThenRests() => Sta.Run(ui => WithEffects(true, () =>
        {
            var t = new YANTg { Size = new Size(90, 40), OffToggleColor = Color.Red, OnToggleColor = Color.Red };
            ui.Show(t);
            t.Checked = true;
            Assert.True(IsSliding(t), "no slide started");
            // no timer tick yet: the toggle is still drawn at the off position although Checked is already true
            Assert.Equal(0f, Knob(t));
            // (the edge column of the circle is fully covered or blended depending on the GDI+ implementation)
            using (var bmp = ui.Render(t))
            {
                Assert.InRange(FirstRedColumn(bmp), 2, 4);
            }
            Assert.True(EndSlide(ui, t), "the slide did not end");
            Assert.Equal(1f, Knob(t));
            using (var bmp = ui.Render(t))
            {
                Assert.InRange(FirstRedColumn(bmp), 90 - 40 + 1, 90 - 40 + 3);
            }
            // reversed while sliding: the new slide starts where the toggle is
            t.Checked = false;
            ui.Pump(3);
            var knob = Knob(t);
            t.Checked = true;
            if (IsSliding(t))
            {
                Assert.Equal(knob, Priv.Field<float>(t, "_knobFrom"));
            }
            Assert.True(EndSlide(ui, t), "the reversed slide did not end");
            Assert.Equal(1f, Knob(t));
        }));

        // A slide survives a new window handle, and disposing the toggle while it slides disposes the timer
        [Fact]
        public void Slide_HandleRecreation_AndDispose() => Sta.Run(ui => WithEffects(true, () =>
        {
            var t = new YANTg();
            ui.Show(t);
            t.Checked = true;
            Priv.Call(t, "RecreateHandle");
            Assert.True(EndSlide(ui, t), "the slide did not end after RecreateHandle");
            Assert.Equal(1f, Knob(t));
            t.Checked = false;
            Assert.True(IsSliding(t));
            var timer = Priv.Field<System.Windows.Forms.Timer>(t, "_timerSlide");
            t.Dispose();
            Assert.Null(Priv.Field<System.Windows.Forms.Timer>(t, "_timerSlide"));
            Assert.False(timer.Enabled);
            ui.Pump();
        }));

        // Pixel sizes are 96-dpi units: at 192 dpi the insets and the toggle scale (a DPI-unaware application stays at 96 dpi)
        [Fact]
        public void Dpi_ScalesTheInsetsAndTheToggle() => Sta.Run(ui =>
        {
            Bitmap Render()
            {
                var t = new YANTg { Size = new Size(90, 40), OffBackColor = Color.Blue, OffToggleColor = Color.Red };
                return ui.Render(t);
            }
            using (var bmp = Render())
            {
                // the toggle spans x = 2..37 and the surface x = 0..88
                Assert.True(Gdi.IsRed(bmp.GetPixel(36, 19)), "96 dpi: toggle expected at x=36, got " + bmp.GetPixel(36, 19));
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(87, 20), "96 dpi: surface at x=87");
            }
            YANPaint.DpiOverride = 192;
            try
            {
                using var bmp = Render();
                // inset 4: the toggle spans x = 4..34 and the surface x = 0..86
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(36, 19), "192 dpi: surface right of the toggle");
                Assert.True(Gdi.IsRed(bmp.GetPixel(19, 19)), "192 dpi: toggle expected at x=19, got " + bmp.GetPixel(19, 19));
                Assert.False(Gdi.Same(bmp.GetPixel(87, 20), Color.Blue), "192 dpi: x=87 is right of the surface");
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // High contrast mode paints system colors: a highlighted surface when on, an outline in the text color when off, through
        // which the parent shows (a lime panel that paints itself: the hidden host form has no window, so it paints nothing)
        [Fact]
        public void HighContrast_PaintsSystemColors() => Sta.Run(ui =>
        {
            YANPaint.HighContrastOverride = true;
            try
            {
                var on = new YANTg { Size = new Size(90, 40), Checked = true };
                var pnl = AllControlsTests.OnParent(ui, new PaintingTests.ColorPanel(Color.Lime), on, Point.Empty);
                using (var bmp = ui.Render(on))
                {
                    PaintingTests.AssertColor(SystemColors.Highlight, bmp.GetPixel(20, 20), "on: surface");
                    PaintingTests.AssertColor(SystemColors.HighlightText, bmp.GetPixel(70, 20), "on: toggle");
                }
                var off = new YANTg { Size = new Size(90, 40) };
                AllControlsTests.OnParent(ui, pnl, off, new Point(0, 50));
                using (var bmp = ui.Render(off))
                {
                    PaintingTests.AssertColor(SystemColors.ControlText, bmp.GetPixel(19, 20), "off: toggle");
                    PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(70, 20), "off: outlined surface shows the parent");
                }
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
        });

        private static Rectangle Row(Rectangle rect, int y) => new(rect.X, y, rect.Width, 1);

        // Counts the lime pixels of a part of the bitmap
        private static int CountLime(Bitmap bmp, Rectangle rect) => Count(bmp, rect, Gdi.IsLime);

        // Counts the matching pixels of a part of the bitmap
        private static int Count(Bitmap bmp, Rectangle rect, Func<Color, bool> match)
        {
            var n = 0;
            for (var x = rect.Left; x < rect.Right; x++)
            {
                for (var y = rect.Top; y < rect.Bottom; y++)
                {
                    if (match(bmp.GetPixel(x, y)))
                    {
                        n++;
                    }
                }
            }
            return n;
        }

        private static float Knob(YANTg t) => (float)Priv.Call(t, "GetKnob");

        private static bool IsSliding(YANTg t) => Priv.Property<bool>(t, "IsSliding");

        // Pumps messages until the slide ends (bounded)
        private static bool EndSlide(Ui ui, YANTg t)
        {
            var sw = Stopwatch.StartNew();
            while (IsSliding(t) && sw.ElapsedMilliseconds < 3000)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
            ui.ThrowIfFailed();
            return !IsSliding(t);
        }

        // Runs the body with Windows animation effects forced on or off (the per-thread override shared with the fades)
        private static void WithEffects(bool on, Action body)
        {
            YANDisplay.UIEffectsOverride = on;
            try
            {
                body();
            }
            finally
            {
                YANDisplay.UIEffectsOverride = null;
            }
        }

        // First column of the row through the middle of the toggle that is red
        private static int FirstRedColumn(Bitmap bmp)
        {
            for (var x = 0; x < bmp.Width; x++)
            {
                if (Gdi.IsRed(bmp.GetPixel(x, bmp.Height / 2)))
                {
                    return x;
                }
            }
            return -1;
        }

        private static void PressSpace(YANTg t)
        {
            Priv.Call(t, "OnKeyDown", new KeyEventArgs(Keys.Space));
            Priv.Call(t, "OnKeyUp", new KeyEventArgs(Keys.Space));
        }

        // A designer site: DesignMode is true for the sited control
        private sealed class DesignSite : ISite
        {
            public IComponent Component => null;

            public IContainer Container => null;

            public bool DesignMode => true;

            public string Name { get; set; } = "yanTg1";

            public object GetService(Type serviceType) => null;
        }
    }
}
