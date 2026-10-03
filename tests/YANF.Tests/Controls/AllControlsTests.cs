using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// Checks that apply to several controls at once.
    /// </summary>
    public class AllControlsTests
    {
        // Every control must construct, create its handle and paint without a parent (Parent.BackColor is not dereferenced)
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANCirPic))]
        [InlineData(typeof(YANGradPnl))]
        [InlineData(typeof(YANPrg))]
        [InlineData(typeof(YANDp))]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANRdo))]
        [InlineData(typeof(YANTxt))]
        [InlineData(typeof(YANNb))]
        [InlineData(typeof(YANDdl))]
        public void NoParent_Draws(Type type) => Sta.Run(ui =>
        {
            using var c = (Control)Activator.CreateInstance(type);
            switch (c)
            {
                case YANPrg prg:
                    prg.Value = 40;
                    break;
                case YANRdo rdo:
                    rdo.Text = "x";
                    break;
            }
            c.CreateControl();
            ui.DrawDetached(c);
        });

        // The hand cursor comes from the protected DefaultCursor: the Cursor property falls back to it, and the designer
        // writes nothing (1.0.2 assigned Cursor on every mouse move)
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANRdo))]
        public void DefaultCursor_IsHand(Type type) => Sta.Run(() =>
        {
            using var c = (Control)Activator.CreateInstance(type);
            Assert.Same(Cursors.Hand, Priv.Property<Cursor>(c, "DefaultCursor"));
            Assert.Same(Cursors.Hand, c.Cursor);
            Assert.False(TypeDescriptor.GetProperties(c)["Cursor"].ShouldSerializeValue(c), "Cursor must not be serialized by the designer");
        });

        // The border smoothing of the composite controls uses Parent.BackColor, so a parent color change must repaint them
        [Fact]
        public void Composite_ParentBackColorChange_Repaints() => Sta.Run(ui =>
        {
            var t = new YANTxt { BorderRadius = 10 };
            var n = new YANNb { BorderRadius = 10 };
            var frm = ui.Show(t, n);
            var txtInvalidated = 0;
            var nbInvalidated = 0;
            t.Invalidated += (s, e) => txtInvalidated++;
            n.Invalidated += (s, e) => nbInvalidated++;
            frm.BackColor = Color.Red;
            Assert.True(txtInvalidated > 0, "YANTxt not invalidated on a parent BackColor change");
            Assert.True(nbInvalidated > 0, "YANNb not invalidated on a parent BackColor change");
            Assert.Equal(Color.White, t.BackColor);
            Assert.Equal(Color.White, n.BackColor);
        });

        // 1.0 forced TabStop = false in these constructors. The designer writes TabStop whenever it differs from its
        // [DefaultValue] (true, but false for RadioButton), so every 1.0 designer file already sets TabStop = false explicitly
        // and existing forms keep their tab order; a new control is at the default, so the designer writes nothing for it
        [Theory]
        [InlineData(typeof(YANBtn), true)]
        [InlineData(typeof(YANTg), true)]
        [InlineData(typeof(YANRdo), false)]
        [InlineData(typeof(YANDp), true)]
        public void TabStop_AtItsDesignerDefault(Type type, bool expected) => Sta.Run(() =>
        {
            using var c = (Control)Activator.CreateInstance(type);
            var prop = TypeDescriptor.GetProperties(c)["TabStop"];
            Assert.Equal(expected, ((DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]).Value);
            Assert.Equal(expected, c.TabStop);
            Assert.False(prop.ShouldSerializeValue(c), "TabStop must not be written for a new control");
            // what a 1.0 designer file does
            c.TabStop = false;
            Assert.False(c.TabStop);
        });

        // The focus cue follows the focus: a focus change repaints the control (ButtonBase does it for YANBtn, YANTg and
        // YANRdo; YANDp does it itself)
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANTg))]
        [InlineData(typeof(YANRdo))]
        [InlineData(typeof(YANDp))]
        public void FocusChange_Repaints(Type type) => Sta.Run(ui =>
        {
            var c = (Control)Activator.CreateInstance(type);
            ui.Draw(c);
            var invalidated = 0;
            c.Invalidated += (s, e) => invalidated++;
            Priv.Call(c, "OnGotFocus", EventArgs.Empty);
            Assert.True(invalidated > 0, "not repainted when it got the focus");
            invalidated = 0;
            Priv.Call(c, "OnLostFocus", EventArgs.Empty);
            Assert.True(invalidated > 0, "not repainted when it lost the focus");
        });

        // The focus cue is drawn only while the control has the focus and the focus cues are shown. Each control is drawn on a panel
        // that paints itself: on Windows, the first control that asks for its keyboard cues (YANRdo's text) gives the hidden host
        // form a window (Control.ShowKeyboardCues sends WM_CHANGEUISTATE to the top-level window), and from then on the form paints
        // its BackColor behind the transparent controls drawn on it, so two identical controls would differ by their background
        [Theory]
        [InlineData(typeof(YANBtn), typeof(FocusedBtn))]
        [InlineData(typeof(YANTg), typeof(FocusedTg))]
        [InlineData(typeof(YANRdo), typeof(FocusedRdo))]
        [InlineData(typeof(YANDp), typeof(FocusedDp))]
        public void FocusCue_DrawnOnlyWhenFocused(Type plain, Type focused) => Sta.Run(ui =>
        {
            Bitmap Render(Type type)
            {
                var c = Setup((Control)Activator.CreateInstance(type));
                OnParent(ui, new PaintingTests.ColorPanel(Color.Lime), c, Point.Empty);
                return ui.Render(c);
            }
            using var bmpPlain = Render(plain);
            using var bmpFocused = Render(focused);
            Assert.True(Differences(bmpPlain, bmpFocused) > 0, "no focus cue drawn");
            // the same control without the focus: no cue
            using var bmpPlain2 = Render(plain);
            Assert.Equal(0, Differences(bmpPlain, bmpPlain2));
        });

        #region Transparency (2.0)
        // Around their round shapes, YANBtn and YANCirPic show what the parent paints (lime), not its BackColor (white): 1.x cut them
        // with a region and painted a ring of Parent.BackColor along the edge. The inside keeps the control's own color, and the edge is
        // anti-aliased (pixels between the two colors), which a region cannot do
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANCirPic))]
        public void RoundShape_ShowsWhatTheParentPaints_WithAnAntiAliasedEdge(Type type) => Sta.Run(ui =>
        {
            var c = CreateRound(type);
            var pnl = OnParent(ui, new PaintingTests.LimePanel(), c, new Point(30, 20));
            using var bmp = ui.Render(c);
            foreach (var corner in RoundCorners(c))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(corner.X, corner.Y), $"{type.Name} corner {corner}");
            }
            PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(c.Width / 2, c.Height / 2), $"{type.Name} inside");
            Assert.Equal(0, Gdi.Count(bmp, color => Gdi.Same(color, Color.White)));
            Assert.True(Gdi.Count(bmp, IsLimeBlueBlend) > 0, "the edge is not anti-aliased");
            Assert.Equal(Color.White, pnl.BackColor);
        });

        // On a gradient, the corners show the gradient pixel behind them (1.x: a solid ring of the panel's BackColor, #15)
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANCirPic))]
        public void RoundShape_ShowsTheGradientBehind(Type type) => Sta.Run(ui =>
        {
            var c = CreateRound(type);
            var pnl = OnParent(ui, new YANGradPnl { TopColor = Color.Red, BottomColor = Color.Blue, BackColor = Color.White, Size = new Size(300, 200) }, c, new Point(60, 40));
            c.Visible = false;
            using var bmpParent = ui.Render(pnl);
            c.Visible = true;
            using var bmp = ui.Render(c);
            foreach (var corner in RoundCorners(c))
            {
                var behind = bmpParent.GetPixel(c.Left + corner.X, c.Top + corner.Y);
                Assert.False(Gdi.Same(behind, Color.White), "the gradient must differ from the panel's BackColor");
                PaintingTests.AssertColor(behind, bmp.GetPixel(corner.X, corner.Y), $"{type.Name} corner {corner}", 3);
            }
        });

        // Zero, tiny and huge sizes, radii and borders on a painted parent
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANCirPic))]
        [InlineData(typeof(YANDp))]
        public void RoundShape_TinyAndHugeSizes_OnAPaintedParent_Draw(Type type) => Sta.Run(ui =>
        {
            foreach (var size in new[] { new Size(0, 0), new Size(1, 1), new Size(2, 2), new Size(3, 1), new Size(1, 3), new Size(5, 5), new Size(2000, 2000) })
            {
                foreach (var value in new[] { 0, 1, 3, 1000, int.MaxValue })
                {
                    ui.Case($"{type.Name} {size.Width}x{size.Height} border/radius {value}", () =>
                    {
                        var c = Sweep.Sized(CreateRound(type), size);
                        switch (c)
                        {
                            case YANBtn btn:
                                btn.BorderRadius = value;
                                btn.BorderSize = value;
                                break;
                            case YANCirPic pic:
                                pic.BorderSize = value;
                                break;
                            case YANDp dp:
                                dp.BorderSize = value;
                                dp.SkinColor = Color.Transparent;
                                break;
                        }
                        OnParent(ui, new PaintingTests.LimePanel(), c, new Point(5, 5));
                        ui.DrawAndDispose(c);
                    });
                }
            }
        });

        // Moving the control to another parent shows the new parent; a new window handle changes nothing
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANCirPic))]
        public void RoundShape_ParentChange_AndHandleRecreation(Type type) => Sta.Run(ui =>
        {
            var c = CreateRound(type);
            OnParent(ui, new PaintingTests.LimePanel(), c, new Point(10, 10));
            var corner = RoundCorners(c)[0];
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(corner.X, corner.Y), "first parent");
            }
            var pnlRed = OnParent(ui, new PaintingTests.ColorPanel(Color.Red), c, new Point(20, 5));
            Assert.Same(pnlRed, c.Parent);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(corner.X, corner.Y), "second parent");
            }
            var handle = c.Handle;
            Priv.Call(c, "RecreateHandle");
            Assert.NotEqual(handle, c.Handle);
            using (var bmp = ui.Render(c))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(corner.X, corner.Y), "after RecreateHandle");
            }
            // without a parent the corners take the control's BackColor (never black)
            pnlRed.Controls.Remove(c);
            c.BackColor = Color.Yellow;
            using (var bmp = new Bitmap(c.Width, c.Height))
            {
                c.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
                ui.ThrowIfFailed();
                PaintingTests.AssertColor(Color.Yellow, bmp.GetPixel(corner.X, corner.Y), "no parent");
            }
            c.Dispose();
        });

        // The control repaints when it moves and when the parent is invalidated behind it (the corners follow the parent)
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANCirPic))]
        public void RoundShape_FollowsTheParent(Type type) => Sta.Run(ui =>
        {
            var c = CreateRound(type);
            var pnl = new PaintingTests.LimePanel();
            ui.Show(pnl);
            pnl.Controls.Add(c);
            c.Location = new Point(10, 10);
            ui.Pump();
            var invalidated = 0;
            c.Invalidated += (s, e) => invalidated++;
            pnl.Invalidate(new Rectangle(0, 0, 20, 20));
            Assert.True(invalidated > 0, "not repainted when the parent was invalidated behind it");
            invalidated = 0;
            c.Location = new Point(40, 30);
            Assert.True(invalidated > 0, "not repainted when it moved");
        });

        // The corners let the clicks through to the parent (WM_NCHITTEST answers HTTRANSPARENT), as the 1.x region did; the inside
        // takes them
        [Theory]
        [InlineData(typeof(YANBtn))]
        [InlineData(typeof(YANCirPic))]
        public void RoundShape_CornersLetTheClicksThrough(Type type) => Sta.Run(ui =>
        {
            var c = CreateRound(type);
            OnParent(ui, new PaintingTests.LimePanel(), c, new Point(30, 20));
            ui.Draw(c);
            Assert.Equal(HTTRANSPARENT, HitTest(c, new Point(1, 1)));
            Assert.Equal(HTTRANSPARENT, HitTest(c, new Point(c.Width - 2, c.Height - 2)));
            Assert.NotEqual(HTTRANSPARENT, HitTest(c, new Point(c.Width / 2, c.Height / 2)));
        });
        #endregion

        // Same settings for the plain and the focused instance (a fixed date: the default Value is DateTime.Now)
        private static Control Setup(Control c)
        {
            c.Size = new Size(150, 40);
            switch (c)
            {
                case YANRdo rdo:
                    rdo.Text = "radio";
                    break;
                case YANDp dp:
                    dp.Value = new DateTime(2026, 9, 29);
                    break;
            }
            return c;
        }

        /// <summary>
        /// The result of WM_NCHITTEST for a point outside a window's shape: the system looks for the window below it.
        /// </summary>
        internal const int HTTRANSPARENT = -1;

        /// <summary>
        /// A YANBtn (150 x 40, blue, radius 20, no text) or a YANCirPic (100 x 100, a blue gradient, no border and no image): blue
        /// inside, round corners.
        /// </summary>
        internal static Control CreateRound(Type type) => type == typeof(YANBtn)
            ? new YANBtn { Size = new Size(150, 40), BackColor = Color.Blue, Text = "", BorderRadius = 20 }
            : type == typeof(YANCirPic)
                ? new YANCirPic { Size = new Size(100, 100), TopColor = Color.Blue, BottomColor = Color.Blue, BorderSize = 0 }
                : new YANDp { Size = new Size(150, 35), Value = new DateTime(2026, 9, 29) };

        /// <summary>
        /// Pixels outside the round shape of <see cref="CreateRound"/>: the four corners, two pixels in, and the middle of the top
        /// edge (1.x painted its Parent.BackColor ring there).
        /// </summary>
        internal static Point[] RoundCorners(Control c) => new[]
        {
            new Point(1, 1), new Point(c.Width - 2, 1), new Point(1, c.Height - 2), new Point(c.Width - 2, c.Height - 2), new Point(c.Width / 2, 0)
        };

        /// <summary>
        /// Puts the control at a location on a panel of the hidden host form.
        /// </summary>
        internal static T OnParent<T>(Ui ui, T pnl, Control c, Point location) where T : Control
        {
            if (pnl.Parent == null)
            {
                ui.Host.Controls.Add(pnl);
            }
            c.Location = location;
            pnl.Controls.Add(c);
            return pnl;
        }

        /// <summary>
        /// A blend of lime and blue: an anti-aliased edge between the parent and the control.
        /// </summary>
        internal static bool IsLimeBlueBlend(Color color) => color.R < 40 && color.G is > 30 and < 225 && color.B is > 30 and < 225;

        /// <summary>
        /// Some lime blended into another color that has no green (blue, red, black): an anti-aliased edge of a lime line or dot.
        /// </summary>
        internal static bool IsPartlyLime(Color color) => color.G > 40 && !Gdi.IsLime(color);

        /// <summary>
        /// Sends WM_NCHITTEST for a point (client coordinates) through the control's window procedure and returns the result.
        /// </summary>
        internal static int HitTest(Control c, Point point)
        {
            var screen = c.PointToScreen(point);
            var args = new object[] { Message.Create(c.Handle, 0x0084, IntPtr.Zero, (IntPtr)((screen.Y << 16) | (screen.X & 0xFFFF))) };
            typeof(Control).GetMethod("WndProc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(c, args);
            return ((Message)args[0]).Result.ToInt32();
        }

        /// <summary>
        /// Counts the pixels that differ between two bitmaps of the same size.
        /// </summary>
        internal static int Differences(Bitmap a, Bitmap b)
        {
            Assert.Equal(a.Size, b.Size);
            var n = 0;
            for (var x = 0; x < a.Width; x++)
            {
                for (var y = 0; y < a.Height; y++)
                {
                    if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb())
                    {
                        n++;
                    }
                }
            }
            return n;
        }

        // The controls below paint as if they had the keyboard focus with the focus cues shown: a real keyboard focus needs an
        // active window and keyboard use, which a test cannot rely on
        internal sealed class FocusedBtn : YANBtn
        {
            public override bool Focused => true;

            // shown, except while the base class paints (what YANBtn itself does with the real value)
            protected override bool ShowFocusCues => !Priv.Field<bool>(this, "_is_PaintingBase");
        }

        internal sealed class FocusedTg : YANTg
        {
            public override bool Focused => true;

            protected override bool ShowFocusCues => true;
        }

        internal sealed class FocusedRdo : YANRdo
        {
            public override bool Focused => true;

            protected override bool ShowFocusCues => true;
        }

        internal sealed class FocusedDp : YANDp
        {
            public override bool Focused => true;

            protected override bool ShowFocusCues => true;
        }
    }
}
