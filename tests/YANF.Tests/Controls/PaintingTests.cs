using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// The shared painting infrastructure of 2.0 (YANPaint, YANBorder) and the transparency of the controls that use it:
    /// the parent's own pixels show behind the control (not a ring or a block of Parent.BackColor), shapes are anti-aliased
    /// without a region, and pixel values scale with the DPI.
    /// </summary>
    public class PaintingTests
    {
        #region Transparency
        // The corners show what the parent paints (lime), not its BackColor (white): 1.x painted a Parent.BackColor ring or block
        [Theory]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANPrg))]
        [InlineData(typeof(YANRdo))]
        public void Corners_ShowWhatTheParentPaints(Type type) => Sta.Run(ui =>
        {
            var c = Create(type);
            var pnl = Parent(ui, new LimePanel(), c, new Point(30, 20));
            using var bmp = ui.Render(c);
            foreach (var corner in Corners(c))
            {
                AssertColor(Color.Lime, bmp.GetPixel(corner.X, corner.Y), $"{type.Name} corner {corner}");
            }
            Assert.Equal(Color.White, pnl.BackColor);
        });

        // On a gradient, every corner shows the gradient pixel behind it (1.x: a solid ring in the panel's BackColor, #15)
        [Theory]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANPrg))]
        [InlineData(typeof(YANRdo))]
        public void Corners_ShowTheGradientBehind(Type type) => Sta.Run(ui =>
        {
            var c = Create(type);
            var pnl = Parent(ui, new YANGradPnl { TopColor = Color.Red, BottomColor = Color.Blue, BackColor = Color.White, Size = new Size(300, 200) }, c, new Point(60, 50));
            c.Visible = false;
            using var bmpParent = ui.Render(pnl);
            c.Visible = true;
            using var bmp = ui.Render(c);
            foreach (var corner in Corners(c))
            {
                var behind = bmpParent.GetPixel(c.Left + corner.X, c.Top + corner.Y);
                Assert.False(Gdi.Same(behind, Color.White), "the gradient must differ from the panel's BackColor");
                AssertColor(behind, bmp.GetPixel(corner.X, corner.Y), $"{type.Name} corner {corner}", 3);
            }
        });

        // The interior still shows the control's own colors
        [Fact]
        public void Interior_ShowsTheControlColors() => Sta.Run(ui =>
        {
            var tg = new YANTg { Size = new Size(90, 40), OffBackColor = Color.Blue, OffToggleColor = Color.Red };
            var prg = new YANPrg { Size = new Size(200, 40), ChannelColor = Color.Red, SliderColor = Color.Blue, TextAlign = YANF.Script.YANConstant.PrgTextPosition.None, Value = 50 };
            var rdo = new YANRdo { Size = new Size(200, 40), Checked = true, CheckedColor = Color.Red };
            Parent(ui, new LimePanel(), tg, new Point(10, 10));
            prg.Location = new Point(10, 60);
            rdo.Location = new Point(10, 110);
            tg.Parent.Controls.Add(prg);
            tg.Parent.Controls.Add(rdo);
            using (var bmp = ui.Render(tg))
            {
                AssertColor(Color.Blue, bmp.GetPixel(70, 20), "toggle surface");
                AssertColor(Color.Red, bmp.GetPixel(19, 20), "toggle knob");
            }
            using (var bmp = ui.Render(prg))
            {
                AssertColor(Color.Blue, bmp.GetPixel(50, 37), "slider");
                AssertColor(Color.Red, bmp.GetPixel(150, 37), "channel");
                AssertColor(Color.Lime, bmp.GetPixel(150, 10), "above the channel");
            }
            using (var bmp = ui.Render(rdo))
            {
                // the checked dot is centred in the 18 px circle at x = 0.5
                AssertColor(Color.Red, bmp.GetPixel(9, 20), "checked dot");
            }
        });

        // A parentless control paints its BackColor, over the system control color when it is not opaque (never black)
        [Fact]
        public void NoParent_PaintsTheBackColor() => Sta.Run(ui =>
        {
            foreach (var (c, expected, what) in new (Control, Color, string)[]
            {
                (new YANTg { Size = new Size(90, 40), BackColor = Color.Red }, Color.Red, "opaque BackColor"),
                (new YANTg { Size = new Size(90, 40), BackColor = Color.Transparent }, SystemColors.Control, "transparent BackColor"),
                (new YANRdo { Size = new Size(90, 40), BackColor = Color.Red }, Color.Red, "YANRdo BackColor"),
                (new YANPrg { Size = new Size(90, 40), BackColor = Color.Red, TextAlign = YANF.Script.YANConstant.PrgTextPosition.Center }, Color.Red, "YANPrg BackColor")
            })
            {
                using (c)
                {
                    c.CreateControl();
                    using var bmp = new Bitmap(c.Width, c.Height);
                    c.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                    ui.ThrowIfFailed();
                    AssertColor(expected, bmp.GetPixel(Corners(c)[0].X, Corners(c)[0].Y), what);
                }
            }
        });

        // Zero, tiny and huge sizes on a painted parent
        [Theory]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANPrg))]
        [InlineData(typeof(YANRdo))]
        public void TinySizes_OnAPaintedParent_Draw(Type type) => Sta.Run(ui =>
        {
            foreach (var size in new[] { new Size(0, 0), new Size(1, 1), new Size(2, 2), new Size(3, 1), new Size(1, 3), new Size(2000, 2000) })
            {
                ui.Case($"{type.Name} {size.Width}x{size.Height}", () =>
                {
                    var c = Sweep.Sized(Create(type), size);
                    Parent(ui, new LimePanel(), c, new Point(5, 5));
                    ui.DrawAndDispose(c);
                });
            }
        });

        // Moving the control to another parent shows the new parent; a new window handle changes nothing
        [Theory]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANPrg))]
        [InlineData(typeof(YANRdo))]
        public void ParentChange_AndHandleRecreation(Type type) => Sta.Run(ui =>
        {
            var c = Create(type);
            Parent(ui, new LimePanel(), c, new Point(10, 10));
            var corner = Corners(c)[0];
            using (var bmp = ui.Render(c))
            {
                AssertColor(Color.Lime, bmp.GetPixel(corner.X, corner.Y), "first parent");
            }
            var pnlRed = Parent(ui, new ColorPanel(Color.Red), c, new Point(20, 5));
            Assert.Same(pnlRed, c.Parent);
            using (var bmp = ui.Render(c))
            {
                AssertColor(Color.Red, bmp.GetPixel(corner.X, corner.Y), "second parent");
            }
            var handle = c.Handle;
            Priv.Call(c, "RecreateHandle");
            Assert.NotEqual(handle, c.Handle);
            using (var bmp = ui.Render(c))
            {
                AssertColor(Color.Red, bmp.GetPixel(corner.X, corner.Y), "after RecreateHandle");
            }
            pnlRed.Controls.Remove(c);
            c.BackColor = Color.Blue;
            ui.DrawDetached(c);
            c.Dispose();
        });

        // A parent whose painting paints the control again (here with DrawToBitmap from its Paint event) does not recurse
        // forever: the nested paint of the control uses its BackColor
        [Fact]
        public void ReentrantParentPaint_Terminates() => Sta.Run(ui =>
        {
            var t = new YANTg { Size = new Size(90, 40) };
            var pnl = Parent(ui, new LimePanel(), t, new Point(10, 10));
            var nested = 0;
            pnl.Paint += (s, e) =>
            {
                if (++nested > 50)
                {
                    throw new InvalidOperationException("the parent's paint recursed");
                }
                using var bmp = new Bitmap(t.Width, t.Height);
                t.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
            };
            using (ui.Render(t))
            {
            }
            Assert.InRange(nested, 1, 2);
        });

        // FollowParent: the control repaints when it moves and when the part of the parent behind it is invalidated, and stops
        // following a parent it has left
        [Fact]
        public void FollowParent_RepaintsOnParentInvalidationAndMove() => Sta.Run(ui =>
        {
            var t = new YANTg { Size = new Size(90, 40) };
            var pnl = new LimePanel { Size = new Size(300, 200) };
            ui.Show(pnl);
            pnl.Controls.Add(t);
            t.Location = new Point(10, 10);
            ui.Pump();
            var invalidated = 0;
            t.Invalidated += (s, e) => invalidated++;
            pnl.Invalidate(new Rectangle(0, 0, 20, 20));
            Assert.True(invalidated > 0, "not repainted when the parent was invalidated behind it");
            invalidated = 0;
            pnl.Invalidate(new Rectangle(200, 100, 20, 20));
            Assert.Equal(0, invalidated);
            t.Location = new Point(50, 60);
            Assert.True(invalidated > 0, "not repainted when it moved");
            var pnlOther = new ColorPanel(Color.Red);
            pnl.Parent.Controls.Add(pnlOther);
            pnlOther.Controls.Add(t);
            ui.Pump();
            invalidated = 0;
            pnl.Invalidate();
            Assert.Equal(0, invalidated);
            pnlOther.Invalidate();
            Assert.True(invalidated > 0, "not following the new parent");
        });

        // WinForms lets another thread invalidate a control (it raises Invalidated on that thread) and its transparent children never
        // touch a window handle then. A followed control with a non-client border (a YANCirPic with a BorderStyle) needs its handle to
        // map the invalidated rectangle: from another thread it repaints whole instead, so nothing throws when cross-thread calls are
        // checked (the default under a debugger)
        [Fact]
        public void FollowParent_ParentInvalidatedFromAnotherThread_DoesNotThrow() => Sta.Run(ui =>
        {
            var pnl = new LimePanel { Size = new Size(300, 200) };
            ui.Show(pnl);
            var pic = new YANCirPic { BorderStyle = BorderStyle.FixedSingle, Location = new Point(5, 5), Size = new Size(60, 60) };
            pnl.Controls.Add(pic);
            ui.Pump();
            Assert.True(pic.IsHandleCreated && pnl.IsHandleCreated, "no window handles");
            Assert.NotEqual(pic.Size, pic.ClientSize);
            var invalidated = 0;
            pic.Invalidated += (s, e) => Interlocked.Increment(ref invalidated);
            Exception error = null;
            var check = Control.CheckForIllegalCrossThreadCalls;
            Control.CheckForIllegalCrossThreadCalls = true;
            try
            {
                var worker = new Thread(() =>
                {
                    try
                    {
                        pnl.Invalidate(new Rectangle(0, 0, 20, 20));
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                });
                worker.Start();
                Assert.True(worker.Join(TimeSpan.FromSeconds(30)), "the worker thread did not finish");
            }
            finally
            {
                Control.CheckForIllegalCrossThreadCalls = check;
            }
            Assert.Null(error);
            Assert.True(invalidated > 0, "not repainted when the parent was invalidated behind it");
            ui.Pump();
        });

        // GDI drawing of the parent (TextRenderer with the default flags, which ignores the transform and the clip of a Graphics) is
        // moved and clipped like its GDI+ drawing: text the parent draws far from the control leaves no ghost in it, and text the
        // parent draws behind the control shows at the right place (the parent is painted through the moved and clipped device
        // context, as WinForms paints a transparent BackColor)
        [Fact]
        public void ParentTextRenderer_IsMovedAndClipped() => Sta.Run(ui =>
        {
            var pnl = new LimePanel { Size = new Size(500, 300) };
            using var font = new Font(FontFamily.GenericSansSerif, 20f, FontStyle.Bold);
            pnl.Paint += (s, e) =>
            {
                TextRenderer.DrawText(e.Graphics, "WWW", font, new Point(5, 5), Color.Black);
                TextRenderer.DrawText(e.Graphics, "WWW", font, new Point(300, 105), Color.Black);
            };
            var r = new YANRdo { Text = "radio", Size = new Size(300, 40), ForeColor = Color.Blue };
            Parent(ui, pnl, r, new Point(100, 100));
            static bool IsBlack(Color c) => c.R < 40 && c.G < 40 && c.B < 40;
            r.Visible = false;
            using (var bmpParent = ui.Render(pnl))
            {
                Assert.True(Gdi.Count(bmpParent, IsBlack) > 0, "the parent draws no text");
            }
            r.Visible = true;
            using var bmp = ui.Render(r);
            var ghost = 0;
            var behind = 0;
            for (var y = 0; y < bmp.Height; y++)
            {
                for (var x = 0; x < bmp.Width; x++)
                {
                    if (!IsBlack(bmp.GetPixel(x, y)))
                    {
                        continue;
                    }
                    if (x < 150)
                    {
                        ghost++;
                    }
                    else
                    {
                        behind++;
                    }
                }
            }
            Assert.True(ghost == 0, $"{ghost} pixels of the parent's text drawn at the parent's coordinates inside the control");
            Assert.True(behind > 0, "the parent's text behind the control is missing");
        });

        // The parent's foreground starts from the graphics state its background started from, as when WinForms paints the parent:
        // a transform or a clip that its OnPaintBackground leaves changed does not move or cut its OnPaint
        [Theory]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANPrg))]
        [InlineData(typeof(YANRdo))]
        public void ParentLayers_StartFromTheSameGraphicsState(Type type) => Sta.Run(ui =>
        {
            var c = Create(type);
            Parent(ui, new LeakyPanel(), c, new Point(30, 20));
            using var bmp = ui.Render(c);
            foreach (var corner in Corners(c))
            {
                AssertColor(Color.Lime, bmp.GetPixel(corner.X, corner.Y), $"{type.Name} corner {corner}");
            }
        });

        // A control with a non-client border paints the parent from its client origin (its Location moved by the border), so the
        // parent's pixels line up with the ones around the control
        [Fact]
        public void BorderedControl_PaintsTheParentFromItsClientOrigin() => Sta.Run(ui =>
        {
            var pnl = new QuadrantPanel();
            ui.Host.Controls.Add(pnl);
            // the border puts the client origin at the corner of the blue quadrant (Location alone is 2 pixels up and left, in red)
            var box = new BorderedBox { Location = new Point(QuadrantPanel.Split.X - 2, QuadrantPanel.Split.Y - 2), Size = new Size(40, 30) };
            pnl.Controls.Add(box);
            box.CreateControl();
            var origin = pnl.PointToClient(box.PointToScreen(Point.Empty));
            Assert.NotEqual(box.Location, origin);
            var size = box.ClientSize;
            using var bmp = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                YANPaint.PaintParent(box, g, new Rectangle(Point.Empty, size));
            }
            ui.ThrowIfFailed();
            AssertColor(QuadrantPanel.ColorAt(origin), bmp.GetPixel(0, 0), $"client origin {origin} (Location {box.Location})");
            AssertColor(QuadrantPanel.ColorAt(new Point(origin.X + 1, origin.Y + 1)), bmp.GetPixel(1, 1), "second pixel");
            AssertColor(QuadrantPanel.ColorAt(new Point(origin.X + size.Width - 1, origin.Y + size.Height - 1)), bmp.GetPixel(size.Width - 1, size.Height - 1), "last pixel");
        });
        #endregion

        #region YANPaint
        // The rounding of Control.LogicalToDeviceUnits (Math.Round, to even), without overflow for huge designer values
        [Theory]
        [InlineData(5, 96, 5)]
        [InlineData(0, 144, 0)]
        [InlineData(1, 144, 2)]
        [InlineData(2, 144, 3)]
        [InlineData(3, 144, 4)]
        [InlineData(18, 192, 36)]
        [InlineData(-5, 144, -8)]
        [InlineData(7, 120, 9)]
        [InlineData(int.MaxValue, 192, int.MaxValue)]
        [InlineData(int.MinValue, 192, int.MinValue)]
        public void LogicalToDevice_RoundsLikeWinForms(int value, int dpi, int expected) => Assert.Equal(expected, YANPaint.LogicalToDevice(value, dpi));

        [Fact]
        public void LogicalToDevice_Float_AndTheControlsDpi() => Sta.Run(() =>
        {
            Assert.Equal(2.4f, YANPaint.LogicalToDevice(1.6f, 144), 3);
            Assert.Equal(1.6f, YANPaint.LogicalToDevice(1.6f, 96));
            using var c = new YANTg();
            YANPaint.DpiOverride = 192;
            try
            {
                Assert.Equal(192, YANPaint.GetDpi(c));
                Assert.Equal(36, YANPaint.LogicalToDevice(c, 18));
                Assert.Equal(3.2f, YANPaint.LogicalToDevice(c, 1.6f), 3);
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
            Assert.True(YANPaint.GetDpi(c) >= 96);
        });

        [Fact]
        public void Lerp_ClampsAndKeepsTheEnds()
        {
            Assert.Equal(Color.Red, YANPaint.Lerp(Color.Red, Color.Blue, -1f));
            Assert.Equal(Color.Red, YANPaint.Lerp(Color.Red, Color.Blue, 0f));
            Assert.Equal(Color.Blue, YANPaint.Lerp(Color.Red, Color.Blue, 1f));
            Assert.Equal(Color.Blue, YANPaint.Lerp(Color.Red, Color.Blue, 2f));
            Assert.Equal(Color.FromArgb(255, 128, 0, 128).ToArgb(), YANPaint.Lerp(Color.Red, Color.Blue, 0.5f).ToArgb());
            // alpha is blended too: Transparent is white with alpha 0
            Assert.Equal(Color.FromArgb(128, 128, 128, 128).ToArgb(), YANPaint.Lerp(Color.Transparent, Color.Black, 0.5f).ToArgb());
        }

        [Fact]
        public void Contrast_PicksTheSystemColorInHighContrast() => Sta.Run(() =>
        {
            YANPaint.HighContrastOverride = true;
            try
            {
                Assert.True(YANPaint.IsHighContrast);
                Assert.Equal(SystemColors.WindowFrame, YANPaint.Contrast(Color.Red, SystemColors.WindowFrame));
                YANPaint.HighContrastOverride = false;
                Assert.Equal(Color.Red, YANPaint.Contrast(Color.Red, SystemColors.WindowFrame));
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
        });

        // The border path lies inside the rectangle, and nothing is built when nothing fits
        [Fact]
        public void BorderPath_LiesInsideTheRectangle()
        {
            using (var path = YANPaint.BorderPath(new RectangleF(0, 0, 100, 40), 20, 4))
            {
                Assert.Equal(new Rectangle(2, 2, 96, 36), Rectangle.Round(path.GetBounds()));
            }
            using (var path = YANPaint.BorderPath(new RectangleF(0, 0, 100, 40), 1000, 1000))
            {
                Assert.Null(path);
            }
            Assert.Null(YANPaint.BorderPath(new RectangleF(0, 0, 100, 40), 5, 0));
            Assert.Null(YANPaint.BorderPath(RectangleF.Empty, 5, 2));
        }

        // The ring has the outline and the inset outline; the whole shape when the width reaches the middle
        [Fact]
        public void BorderRing_OutlineAndInsetOutline()
        {
            using (var ring = YANPaint.BorderRing(new RectangleF(0, 0, 100, 40), 20, 4))
            {
                Assert.Equal(new Rectangle(0, 0, 100, 40), Rectangle.Round(ring.GetBounds()));
                Assert.True(ring.IsVisible(1, 20));
                Assert.False(ring.IsVisible(50, 20));
            }
            using (var ring = YANPaint.BorderRing(new RectangleF(0, 0, 100, 40), 0, 1000))
            {
                Assert.True(ring.IsVisible(50, 20));
            }
            Assert.Null(YANPaint.BorderRing(new RectangleF(0, 0, 100, 40), 5, 0));
            Assert.Null(YANPaint.BorderRing(RectangleF.Empty, 5, 2));
        }

        [Fact]
        public void FillAndDrawPath_RestoreTheSmoothingAndPixelOffsetModes()
        {
            using var bmp = new Bitmap(20, 20);
            using var g = Graphics.FromImage(bmp);
            using var path = YANF.Script.YANShape.RoundedRect(new RectangleF(0, 0, 20, 20), 5);
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            YANPaint.FillPath(g, Color.Red, path);
            YANPaint.DrawPath(g, Color.Blue, 2, path);
            YANPaint.FillPath(g, Color.Red, null);
            YANPaint.DrawPath(g, Color.Blue, 0, path);
            Assert.Equal(SmoothingMode.None, g.SmoothingMode);
            Assert.Equal(PixelOffsetMode.HighSpeed, g.PixelOffsetMode);
            AssertColor(Color.Red, bmp.GetPixel(10, 10), "filled");
        }

        // YANPaint and YANBorder fill in the same pixel model: the same shape covers the same pixels, so a surface filled with one lines
        // up with a border painted by the other (the default pixel offset fills half a pixel up and left)
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void FillPath_CoversTheSamePixelsAsTheBorderShape(int radius) => Sta.Run(() =>
        {
            using var box = new BorderBox { Size = new Size(60, 30) };
            box.Border.Radius = radius;
            using var viaBorder = new Bitmap(60, 30);
            using var viaPaint = new Bitmap(60, 30);
            using (var g = Graphics.FromImage(viaBorder))
            {
                g.Clear(Color.Lime);
                box.Border.FillShape(g, Color.Blue);
            }
            using (var g = Graphics.FromImage(viaPaint))
            {
                g.Clear(Color.Lime);
                YANPaint.FillPath(g, Color.Blue, box.Border.Shape);
            }
            Assert.Equal(0, AllControlsTests.Differences(viaBorder, viaPaint));
            AssertColor(radius == 0 ? Color.Blue : Color.Lime, viaPaint.GetPixel(0, 0), "corner pixel");
        });

        // Reduced motion: the tests' override wins; without it nothing is animated while the UI effects are off, and reading the
        // second switch (the "Animation effects" of Windows 10 and 11) never throws, even without user32 (Mono)
        [Fact]
        public void IsAnimated_FollowsTheOverride_AndTheWindowsSwitches() => Sta.Run(() =>
        {
            try
            {
                YANF.Script.YANDisplay.UIEffectsOverride = false;
                Assert.False(YANPaint.IsAnimated);
                YANF.Script.YANDisplay.UIEffectsOverride = true;
                Assert.True(YANPaint.IsAnimated);
            }
            finally
            {
                YANF.Script.YANDisplay.UIEffectsOverride = null;
            }
            var isAnimated = YANPaint.IsAnimated;
            Assert.True(SystemInformation.UIEffectsEnabled || !isAnimated, "animated while the UI effects are off");
            Assert.Equal(isAnimated, YANPaint.IsAnimated);
        });
        #endregion

        #region YANBorder
        // Anti-aliased rounded shape over the painted parent: lime corners, a red border band, a blue surface
        [Fact]
        public void Border_PaintsOverTheParent() => Sta.Run(ui =>
        {
            var box = BorderOnLime(ui, 20);
            using (var bmp = ui.Render(box))
            {
                AssertColor(Color.Lime, bmp.GetPixel(0, 0), "corner");
                AssertColor(Color.Red, bmp.GetPixel(100, 1), "border, second row");
                AssertColor(Color.Red, bmp.GetPixel(100, 2), "border, third row");
                AssertColor(Color.Red, bmp.GetPixel(197, 30), "border, right");
                AssertColor(Color.Red, bmp.GetPixel(100, 57), "border, bottom");
                AssertColor(Color.Blue, bmp.GetPixel(100, 6), "surface below the border");
                AssertColor(Color.Blue, bmp.GetPixel(193, 30), "surface left of the border");
                AssertColor(Color.Blue, bmp.GetPixel(100, 30), "surface");
            }
            // square corners: the border reaches the corner pixel
            box.Border.Radius = 0;
            using (var bmp = ui.Render(box))
            {
                AssertColor(Color.Red, bmp.GetPixel(0, 0), "square corner");
                AssertColor(Color.Red, bmp.GetPixel(2, 2), "square corner, inside");
                AssertColor(Color.Blue, bmp.GetPixel(6, 6), "square surface");
            }
        });

        // The border is filled, not stroked, under PixelOffsetMode.Half: a band of exactly BorderSize pixels with crisp edges
        [Fact]
        public void Border_BandIsExactlyTheBorderSize() => Sta.Run(ui =>
        {
            var box = BorderOnLime(ui, 20);
            using (var bmp = ui.Render(box))
            {
                for (var i = 0; i < 4; i++)
                {
                    AssertColor(Color.Red, bmp.GetPixel(100, i), $"border, top row {i}");
                    AssertColor(Color.Red, bmp.GetPixel(199 - i, 30), $"border, right column {199 - i}");
                    AssertColor(Color.Red, bmp.GetPixel(100, 59 - i), $"border, bottom row {59 - i}");
                    AssertColor(Color.Red, bmp.GetPixel(i, 30), $"border, left column {i}");
                }
                AssertColor(Color.Blue, bmp.GetPixel(100, 4), "surface below the border");
                AssertColor(Color.Blue, bmp.GetPixel(195, 30), "surface left of the right border");
                AssertColor(Color.Blue, bmp.GetPixel(100, 55), "surface above the bottom border");
                AssertColor(Color.Blue, bmp.GetPixel(4, 30), "surface right of the left border");
            }
            box.Border.Radius = 0;
            using (var bmp = ui.Render(box))
            {
                AssertColor(Color.Red, bmp.GetPixel(3, 3), "square corner, inside");
                AssertColor(Color.Blue, bmp.GetPixel(4, 4), "square surface");
            }
        });

        [Fact]
        public void Border_AllSizesAndShapes_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var (radius, border) in Sweep.PaintShapes)
                {
                    ui.Case($"YANBorder {size.Width}x{size.Height} r={radius} b={border}", () =>
                    {
                        var box = Sweep.Sized(new BorderBox(), size);
                        box.Border.Size = border;
                        box.Border.Radius = radius;
                        Assert.True(box.Border.Size >= 0 && box.Border.Radius >= 0);
                        Assert.InRange(box.Border.DeviceSize, 0, Math.Max(0, Math.Min(size.Width, size.Height) / 2));
                        Assert.InRange(box.Border.DeviceRadius, 0f, Math.Min(size.Width, size.Height) / 2f);
                        Parent(ui, new LimePanel(), box, new Point(3, 3));
                        ui.DrawAndDispose(box);
                    });
                }
            }
        });

        // The shape is built once per size and DPI, not per paint; the configured values are never changed to fit
        [Fact]
        public void Border_CachesPerSizeAndDpi() => Sta.Run(() =>
        {
            using var box = new BorderBox { Size = new Size(100, 40) };
            box.Border.Size = 2;
            box.Border.Radius = 1000;
            var shape = box.Border.Shape;
            Assert.NotNull(shape);
            Assert.Same(shape, box.Border.Shape);
            Assert.Equal(20f, box.Border.DeviceRadius);
            Assert.Equal(1000, box.Border.Radius);
            box.Size = new Size(100, 60);
            var resized = box.Border.Shape;
            Assert.NotSame(shape, resized);
            Assert.True(IsDisposed(shape), "the path built for the old size must be disposed");
            Assert.Equal(30f, box.Border.DeviceRadius);
            YANPaint.DpiOverride = 192;
            try
            {
                Assert.NotSame(resized, box.Border.Shape);
                Assert.Equal(4, box.Border.DeviceSize);
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
            Assert.Equal(YANPaint.LogicalToDevice(box, 2), box.Border.DeviceSize);
            var current = box.Border.Shape;
            box.Border.Reset();
            Assert.True(IsDisposed(current), "Reset must dispose the path");
            Assert.NotSame(current, box.Border.Shape);
            box.Border.Bounds = new Rectangle(10, 10, 20, 20);
            Assert.Equal(new Rectangle(10, 10, 20, 20), Rectangle.Round(box.Border.Shape.GetBounds()));
            Assert.True(box.Border.Contains(new Point(20, 20)));
            Assert.False(box.Border.Contains(new Point(10, 10)));
            box.Border.Bounds = null;
            Assert.Equal(new Rectangle(0, 0, 100, 60), Rectangle.Round(box.Border.Shape.GetBounds()));
        });

        // The underline covers exactly its rows on an anti-aliased graphics (the default pixel offset blurred it over two rows), and
        // the state of the graphics is restored
        [Fact]
        public void Underline_CoversExactlyItsRows_OnAnAntiAliasedGraphics() => Sta.Run(() =>
        {
            using var box = new BorderBox { Size = new Size(40, 20) };
            box.Border.Size = 2;
            using var bmp = new Bitmap(40, 20);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var pixelOffset = g.PixelOffsetMode;
            box.Border.DrawUnderline(g, Color.Red);
            Assert.Equal(SmoothingMode.AntiAlias, g.SmoothingMode);
            Assert.Equal(pixelOffset, g.PixelOffsetMode);
            AssertColor(Color.White, bmp.GetPixel(20, 17), "above the underline");
            AssertColor(Color.Red, bmp.GetPixel(20, 18), "underline, first row");
            AssertColor(Color.Red, bmp.GetPixel(20, 19), "underline, second row");
        });

        [Fact]
        public void Border_NegativeValues_StoredAsZero_AndEmptyBounds() => Sta.Run(() =>
        {
            using var box = new BorderBox { Size = new Size(50, 50) };
            box.Border.Size = -3;
            box.Border.Radius = -5;
            Assert.Equal(0, box.Border.Size);
            Assert.Equal(0, box.Border.Radius);
            Assert.Null(box.Border.Line);
            Assert.False(box.Border.IsRounded);
            box.MinimumSize = Size.Empty;
            box.Size = Size.Empty;
            Assert.Null(box.Border.Shape);
            Assert.Equal(0, box.Border.DeviceSize);
            Assert.False(box.Border.Contains(Point.Empty));
            box.Border.Dispose();
            box.Border.Dispose();
        });
        #endregion

        #region Helpers
        // Two background pixels near the top corners (2 pixels in: Mono paints its own frame over a ProgressBar, even a UserPaint one)
        private static Point[] Corners(Control c) => new[] { new Point(2, 2), new Point(c.Width - 3, 2) };

        private static Control Create(Type type)
        {
            var c = (Control)Activator.CreateInstance(type);
            c.Size = new Size(120, 40);
            switch (c)
            {
                case YANPrg prg:
                    // the value box in the middle of the top, away from the corners (and no progress: Mono paints its own progress
                    // chunks over a ProgressBar, even a UserPaint one)
                    prg.Value = 0;
                    prg.TextAlign = YANF.Script.YANConstant.PrgTextPosition.Center;
                    break;
                case YANRdo rdo:
                    rdo.Text = "radio";
                    break;
            }
            return c;
        }

        // Puts the control on a panel of the hidden host form
        private static T Parent<T>(Ui ui, T pnl, Control c, Point location) where T : Control
        {
            if (pnl.Parent == null)
            {
                ui.Host.Controls.Add(pnl);
            }
            c.Location = location;
            pnl.Controls.Add(c);
            return pnl;
        }

        // A 200 x 60 blue box with a red border of 4 on a lime parent
        private static BorderBox BorderOnLime(Ui ui, int radius)
        {
            var box = new BorderBox { Size = new Size(200, 60), BackColor = Color.Blue };
            box.Border.Size = 4;
            box.Border.Radius = radius;
            Parent(ui, new LimePanel(), box, new Point(10, 10));
            return box;
        }

        // A disposed path no longer answers
        private static bool IsDisposed(GraphicsPath path)
        {
            try
            {
                return path.PointCount < 0;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            catch (NullReferenceException)
            {
                return true;
            }
        }

        internal static void AssertColor(Color expected, Color actual, string what, int tolerance = 0)
        {
            var ok = Math.Abs(expected.R - actual.R) <= tolerance && Math.Abs(expected.G - actual.G) <= tolerance && Math.Abs(expected.B - actual.B) <= tolerance && actual.A == 255;
            Assert.True(ok, $"{what}: expected {expected}, got {actual}");
        }
        #endregion

        #region Nested types
        /// <summary>
        /// A parent whose visible background (lime) differs from its BackColor (white), like a gradient or an image.
        /// </summary>
        internal class LimePanel : Panel
        {
            public LimePanel()
            {
                BackColor = Color.White;
                Size = new Size(300, 200);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                base.OnPaintBackground(e);
                e.Graphics.FillRectangle(Brushes.Lime, ClientRectangle);
            }
        }

        /// <summary>
        /// A parent whose OnPaintBackground leaves a transform and a clip changed; its OnPaint paints lime over its whole client area.
        /// </summary>
        internal sealed class LeakyPanel : Panel
        {
            public LeakyPanel()
            {
                BackColor = Color.White;
                Size = new Size(300, 200);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                base.OnPaintBackground(e);
                e.Graphics.TranslateTransform(1000, 1000);
                e.Graphics.SetClip(new Rectangle(0, 0, 1, 1));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.FillRectangle(Brushes.Lime, ClientRectangle);
            }
        }

        /// <summary>
        /// A parent painted red, with a blue quadrant from <see cref="Split"/> to its bottom-right corner.
        /// </summary>
        internal sealed class QuadrantPanel : Panel
        {
            public static readonly Point Split = new(50, 30);

            public QuadrantPanel() => Size = new Size(300, 200);

            public static Color ColorAt(Point point) => point.X >= Split.X && point.Y >= Split.Y ? Color.Blue : Color.Red;

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.FillRectangle(Brushes.Red, ClientRectangle);
                e.Graphics.FillRectangle(Brushes.Blue, Rectangle.FromLTRB(Split.X, Split.Y, Width, Height));
            }
        }

        /// <summary>
        /// A control with a non-client border (WS_EX_CLIENTEDGE, 2 pixels on Windows).
        /// </summary>
        internal sealed class BorderedBox : UserControl
        {
            public BorderedBox() => BorderStyle = BorderStyle.Fixed3D;
        }

        /// <summary>
        /// A plain panel of one color, which it paints over its ClientRectangle itself: Control.OnPaintBackground fills the client
        /// rectangle of the window (GetClientRect), which is empty for a panel of the hidden host form, as it has no window.
        /// </summary>
        internal sealed class ColorPanel : Panel
        {
            public ColorPanel(Color color)
            {
                BackColor = color;
                Size = new Size(300, 200);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                using var brush = new SolidBrush(BackColor);
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        /// <summary>
        /// The pattern YANBorder documents for a control: parent, anti-aliased surface, border.
        /// </summary>
        internal sealed class BorderBox : Control
        {
            public BorderBox()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Border = new YANBorder(this, 0, 0);
                YANPaint.FollowParent(this);
            }

            public YANBorder Border { get; }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                YANPaint.PaintParent(this, e);
                Border.FillShape(e.Graphics, BackColor);
            }

            protected override void OnPaint(PaintEventArgs e) => Border.DrawBorder(e.Graphics, Color.Red);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Border.Dispose();
                }
                base.Dispose(disposing);
            }
        }
        #endregion
    }
}
