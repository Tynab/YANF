using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    using Control = System.Windows.Forms.Control;

    public class YANBtnTests
    {
        [Fact]
        public void BorderLargerThanRadius_Draws() => Sta.Run(ui => ui.DrawAndDispose(new YANBtn
        {
            BorderSize = 25,
            BorderRadius = 20
        }));

        [Fact]
        public void AllSizesAndShapes_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var (radius, border) in Sweep.PaintShapes)
                {
                    ui.Case($"YANBtn {size.Width}x{size.Height} r={radius} b={border}", () =>
                    {
                        var b = Sweep.Sized(new YANBtn { BorderRadius = radius, BorderSize = border }, size);
                        Assert.Equal(Math.Max(0, radius), b.BorderRadius);
                        Assert.Equal(Math.Max(0, border), b.BorderSize);
                        ui.DrawAndDispose(b);
                    });
                }
            }
        });

        [Fact]
        public void Resize_KeepsConfiguredValues() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = 20, BorderSize = 5 };
            b.Size = new Size(30, 30);
            b.Size = new Size(4, 4);
            b.Size = new Size(0, 0);
            b.Size = new Size(150, 40);
            Assert.Equal(20, b.BorderRadius);
            Assert.Equal(5, b.BorderSize);
            ui.DrawAndDispose(b);
        });

        [Fact]
        public void NegativeValues_BecomeZero() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = -10, BorderSize = -2 };
            Assert.Equal(0, b.BorderRadius);
            Assert.Equal(0, b.BorderSize);
            ui.DrawAndDispose(b);
        });

        // 1.0.2 threw a NullReferenceException from OnHandleCreated when the button had no parent
        [Fact]
        public void NoParent_CreateAndDraw() => Sta.Run(ui =>
        {
            using var b = new YANBtn { BorderRadius = 20, BorderSize = 3 };
            Assert.Null(b.Parent);
            b.CreateControl();
            _ = b.Handle;
            ui.ThrowIfFailed();
            ui.DrawDetached(b);
            b.BorderRadius = 5;
            b.Size = new Size(20, 10);
            ui.DrawDetached(b);
        });

        // 1.0.2 subscribed to Parent.BackColorChanged in OnHandleCreated: every handle recreation added one more handler
        [Fact]
        public void RecreateHandle_NoParentSubscription() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(300, 100) };
            ui.Host.Controls.Add(pnl);
            var b = new YANBtn();
            pnl.Controls.Add(b);
            var handles = 0;
            b.HandleCreated += (s, e) => handles++;
            _ = b.Handle;
            var before = handles;
            // RightToLeft recreates the handle; force it if the runtime does not
            b.RightToLeft = RightToLeft.Yes;
            b.RightToLeft = RightToLeft.No;
            if (handles == before)
            {
                Priv.Call(b, "RecreateHandle");
            }
            Assert.True(handles > before, "the handle was not recreated");
            Assert.DoesNotContain(BackColorHandlers(pnl), d => ReferenceEquals(d.Target, b));
            // the parent's BackColor still reaches the button (OnParentBackColorChanged) without throwing
            pnl.BackColor = Color.Red;
            ui.Draw(b);
            // reparenting must not leave a handler on the old or the new parent either
            var pnl2 = new Panel { Size = new Size(300, 100) };
            ui.Host.Controls.Add(pnl2);
            pnl2.Controls.Add(b);
            Assert.DoesNotContain(BackColorHandlers(pnl).Concat(BackColorHandlers(pnl2)), d => ReferenceEquals(d.Target, b));
        });

        // 2.0: a painted style has no region at all (the parent is painted around the rounded shape), and a region that the
        // application sets is kept (1.x replaced it on every size change)
        [Fact]
        public void PaintedStyles_SetNoRegion_AndKeepTheApplicationsRegion() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = 20, BorderSize = 2, Size = new Size(150, 40) };
            ui.Draw(b);
            Assert.Null(b.Region);
            var changes = 0;
            b.RegionChanged += (s, e) => changes++;
            b.BorderRadius = 10;
            b.Size = new Size(100, 30);
            b.FlatStyle = FlatStyle.Popup;
            b.FlatStyle = FlatStyle.Standard;
            ui.Draw(b);
            Assert.Equal(0, changes);
            using var own = new Region(new Rectangle(0, 0, 50, 20));
            b.Region = own;
            b.Size = new Size(120, 40);
            b.BorderRadius = 5;
            ui.Draw(b);
            Assert.Same(own, b.Region);
            b.Region = null;
        });

        // A FlatStyle.System button is painted by Windows, so its corners are still cut with a region. 1.0.2 assigned a new region in
        // OnPaint, and assigning a region invalidates the window: an endless repaint loop. The region only changes with the shape
        [Fact]
        public void SystemStyle_Region_OnlyChangesWithShape() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = 20, BorderSize = 2, Size = new Size(150, 40), FlatStyle = FlatStyle.System };
            ui.Draw(b);
            Assert.NotNull(b.Region);
            var changes = 0;
            b.RegionChanged += (s, e) => changes++;
            ui.Draw(b);
            ui.Draw(b);
            Assert.Equal(0, changes);
            b.BorderColor = Color.Blue;
            b.BorderSize = 4;
            Assert.Equal(0, changes);
            b.BorderRadius = 10;
            Assert.Equal(1, changes);
            b.BorderRadius = 10;
            Assert.Equal(1, changes);
            b.Size = new Size(100, 30);
            Assert.Equal(2, changes);
            b.BorderRadius = 0;
            Assert.Null(b.Region);
            b.BorderRadius = 20;
            Assert.NotNull(b.Region);
            // back to a painted style: the library's region goes
            b.FlatStyle = FlatStyle.Flat;
            Assert.Null(b.Region);
        });

        // The hand cursor comes from DefaultCursor now; 1.0.2 overwrote Cursor on every mouse move
        [Fact]
        public void UserCursor_NotOverwrittenOnMouseMove() => Sta.Run(() =>
        {
            using var b = new YANBtn { Cursor = Cursors.Cross };
            Priv.Call(b, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));
            Assert.Same(Cursors.Cross, b.Cursor);
        });

        // 1.0 forced TabStop = false and TabIndex = 0. 2.0: a new button takes the focus like a standard Button (Focusable defaults to
        // true; the designer writes Focusable = false, and no longer writes true)
        [Fact]
        public void NewButton_StandardTabStop_Focusable() => Sta.Run(ui =>
        {
            var b1 = new YANBtn();
            var b2 = new YANBtn();
            Assert.True(b1.TabStop);
            Assert.True(b1.Focusable);
            Assert.True(GetStyle(b1, ControlStyles.Selectable));
            var prop = TypeDescriptor.GetProperties(b1)[nameof(YANBtn.Focusable)];
            Assert.Equal(true, ((DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]).Value);
            Assert.False(prop.ShouldSerializeValue(b1), "Focusable must not be written at its default");
            // code-created buttons get the next tab index, like any control (1.0 gave all of them 0)
            var pnl = new Panel();
            ui.Host.Controls.Add(pnl);
            pnl.Controls.Add(b1);
            pnl.Controls.Add(b2);
            Assert.Equal((0, 1), (b1.TabIndex, b2.TabIndex));
            var txt = new TextBox();
            var frm = ui.Show(txt, b1);
            Assert.True(b1.CanSelect);
            Assert.True(frm.SelectNextControl(txt, true, true, true, true));
            Assert.Same(b1, frm.ActiveControl);
        });

        [Fact]
        public void Focusable_WrapsSelectable() => Sta.Run(ui =>
        {
            var b = new YANBtn();
            ui.Show(new TextBox(), b);
            b.Focusable = false;
            Assert.False(b.Focusable);
            Assert.False(GetStyle(b, ControlStyles.Selectable));
            Assert.False(b.CanSelect);
            Assert.True(TypeDescriptor.GetProperties(b)[nameof(YANBtn.Focusable)].ShouldSerializeValue(b), "Focusable = false must be written");
            b.Focusable = true;
            Assert.True(GetStyle(b, ControlStyles.Selectable));
            Assert.True(b.CanSelect);
        });

        // TAB reaches a button when it is Focusable and its TabStop is true (1.0 designer files set TabStop = false)
        [Fact]
        public void Tab_ReachesFocusableButtonsOnly() => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var plain = new YANBtn { Focusable = false };
            var legacy = new YANBtn { TabStop = false };
            var focusable = new YANBtn();
            var frm = ui.Show(txt, plain, legacy, focusable);
            Assert.True(frm.SelectNextControl(txt, true, true, true, true));
            Assert.Same(focusable, frm.ActiveControl);
            Assert.True(frm.SelectNextControl(focusable, true, true, true, true));
            Assert.Same(txt, frm.ActiveControl);
        });

        // 1.0.2 forced ControlStyles.Selectable off, and Button.PerformClick does nothing unless the button can be selected
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PerformClick_ClicksWhetherFocusableOrNot(bool focusable) => Sta.Run(ui =>
        {
            var b = new YANBtn { Focusable = focusable };
            var clicks = 0;
            b.Click += (s, e) => clicks++;
            ui.Show(b);
            b.PerformClick();
            Assert.Equal(1, clicks);
            ((IButtonControl)b).PerformClick();
            Assert.Equal(2, clicks);
            Assert.Equal(focusable, b.Focusable);
            Assert.Equal(focusable, GetStyle(b, ControlStyles.Selectable));
            // like Button.PerformClick, nothing happens while the button is disabled or hidden
            b.Enabled = false;
            b.PerformClick();
            b.Enabled = true;
            b.Visible = false;
            b.PerformClick();
            Assert.Equal(2, clicks);
            Assert.Equal(focusable, GetStyle(b, ControlStyles.Selectable));
        });

        // PerformClick lends the Selectable style for the call only; a Click handler that sets Focusable keeps its value
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PerformClick_KeepsFocusableSetInClickHandler(bool value) => Sta.Run(ui =>
        {
            var b = new YANBtn { Focusable = false };
            bool? inHandler = null;
            b.Click += (s, e) =>
            {
                inHandler = b.Focusable;
                b.Focusable = value;
            };
            ui.Show(b);
            b.PerformClick();
            Assert.False(inHandler);
            Assert.Equal(value, b.Focusable);
            Assert.Equal(value, GetStyle(b, ControlStyles.Selectable));
        });

        // Form.AcceptButton and Form.CancelButton call IButtonControl.PerformClick on ENTER and ESC: a 1.0.2 button never clicked
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AcceptAndCancelButton_ClickOnEnterAndEscape(bool focusable) => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var ok = new YANBtn { Focusable = focusable };
            var cancel = new YANBtn { Focusable = focusable };
            var okClicks = 0;
            var cancelClicks = 0;
            ok.Click += (s, e) => okClicks++;
            cancel.Click += (s, e) => cancelClicks++;
            var frm = ui.Show(txt, ok, cancel);
            frm.AcceptButton = ok;
            frm.CancelButton = cancel;
            frm.ActiveControl = txt;
            Assert.True((bool)Priv.Call(frm, "ProcessDialogKey", Keys.Enter));
            Assert.Equal((1, 0), (okClicks, cancelClicks));
            Assert.True((bool)Priv.Call(frm, "ProcessDialogKey", Keys.Escape));
            Assert.Equal((1, 1), (okClicks, cancelClicks));
            Assert.Equal(focusable, ok.Focusable);
        });

        // Like a standard Button, the accept button validates the focused control before its Click, so a binding in
        // OnValidation mode saves the last edit first; a cancelled validation cancels the click
        [Fact]
        public void AcceptButton_ValidatesTheFocusedControlFirst() => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var ok = new YANBtn();
            var events = "";
            var cancel = true;
            txt.Validating += (s, e) =>
            {
                events += "V";
                e.Cancel = cancel;
            };
            ok.Click += (s, e) => events += "C";
            var frm = ui.Show(txt, ok);
            frm.AcceptButton = ok;
            frm.ActiveControl = txt;
            Priv.Call(frm, "ProcessDialogKey", Keys.Enter);
            Assert.Equal("V", events);
            cancel = false;
            events = "";
            Priv.Call(frm, "ProcessDialogKey", Keys.Enter);
            Assert.Equal("VC", events);
        });

        // Button.ProcessMnemonic calls the inherited PerformClick: 1.0.2 swallowed ALT+S of "&Save" without clicking. The form's
        // mnemonic dispatch reaches the button although it cannot be selected
        [Fact]
        public void Mnemonic_Clicks_WhenNotFocusable() => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var b = new YANBtn { Text = "&Save", Focusable = false };
            var clicks = 0;
            b.Click += (s, e) => clicks++;
            var frm = ui.Show(txt, b);
            frm.ActiveControl = txt;
            Assert.True((bool)Priv.Call(frm, "ProcessMnemonic", 's'));
            Assert.Equal(1, clicks);
            Assert.False((bool)Priv.Call(frm, "ProcessMnemonic", 'x'));
            b.UseMnemonic = false;
            Assert.False((bool)Priv.Call(frm, "ProcessMnemonic", 's'));
            Assert.Equal(1, clicks);
            Assert.False(b.Focusable);
            Assert.Same(txt, frm.ActiveControl);
        });

        // SPACE clicks the button that has the focus (ButtonBase's key handling): TAB gives it the focus only when it is Focusable,
        // otherwise the focus, and SPACE with it, stays on the other control
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Space_ClicksTheButton_OnlyWhenFocusable(bool focusable) => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var b = new YANBtn { Focusable = focusable };
            var clicks = 0;
            b.Click += (s, e) => clicks++;
            var frm = ui.Show(txt, b);
            frm.ActiveControl = txt;
            frm.SelectNextControl(txt, true, true, true, true);
            Assert.Same(focusable ? b : (Control)txt, frm.ActiveControl);
            // the keyboard input goes to the focused control
            var focused = frm.ActiveControl;
            Priv.Call(focused, "OnKeyDown", new KeyEventArgs(Keys.Space));
            Priv.Call(focused, "OnKeyUp", new KeyEventArgs(Keys.Space));
            Assert.Equal(focusable ? 1 : 0, clicks);
        });

        // The focus cue follows the shape at every size and shape without throwing
        [Fact]
        public void FocusCue_AllSizesAndShapes_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var (radius, border) in Sweep.PaintShapes)
                {
                    ui.Case($"focused YANBtn {size.Width}x{size.Height} r={radius} b={border}", () => ui.DrawAndDispose(Sweep.Sized(new AllControlsTests.FocusedBtn { BorderRadius = radius, BorderSize = border, Focusable = true }, size)));
                }
            }
        });

        // The cue is a dotted outline in ForeColor inside the rounded shape (no text: lime pixels can only come from the cue)
        [Fact]
        public void FocusCue_DrawnInForeColor_InsideTheShape() => Sta.Run(ui =>
        {
            using (var bmp = ui.Render(Colored(new YANBtn())))
            {
                Assert.Equal(0, Gdi.Count(bmp, Gdi.IsLime));
            }
            using (var bmp = ui.Render(Colored(new AllControlsTests.FocusedBtn())))
            {
                Assert.True(Gdi.Count(bmp, Gdi.IsLime) > 0, "focus cue not drawn");
                // a sharp dotted line on row 3 (3 pixels from the edge) along the straight part of the top edge
                Assert.True(Count(bmp, new Rectangle(40, 3, 70, 1), Gdi.IsLime) >= 70 / 3, "focus cue not sharp on row 3");
                // inside the shape: nothing in the outer 3 pixels, nor in the corner that the rounded end cuts off (a rectangular
                // cue inset by 3 pixels would cross it)
                Assert.Equal(0, Count(bmp, new Rectangle(0, 0, 150, 3), Gdi.IsLime));
                Assert.Equal(0, Count(bmp, new Rectangle(0, 0, 6, 6), Gdi.IsLime));
            }
        });

        // The flat style's own focus rectangle, which would cross the rounded corners, is not drawn: ShowFocusCues reads false
        // while the base class paints (the cue is drawn inside the shape afterwards)
        [Fact]
        public void BasePaint_SeesNoFocusCues() => Sta.Run(ui =>
        {
            var b = new YANBtn { Focusable = true };
            ui.Draw(b);
            bool? inPaint = null;
            b.Paint += (s, e) => inPaint = Priv.Property<bool>(b, "ShowFocusCues");
            ui.Draw(b);
            Assert.False(inPaint);
        });

        // 2.0 keeps the 1.x geometry at 96 dpi: a rounded button is inset by the ring where 1.x painted Parent.BackColor (one pixel, or
        // half the border rounded up), which now shows the parent; the border lies inside it. A square button fills its whole client
        // area
        [Fact]
        public void Border_KeepsThe1xGeometry() => Sta.Run(ui =>
        {
            var b = OnLime(ui, new YANBtn { BorderRadius = 20, BorderSize = 4, BorderColor = Color.Red });
            using (var bmp = ui.Render(b))
            {
                // the ring of half the border (2 pixels) shows the parent, then 4 pixels of border, then the face
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(75, 0), "ring, first row");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(75, 3), "border");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(75, 4), "border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(75, 8), "face");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(146, 20), "border, right");
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(149, 20), "ring, last column");
            }
            // an odd border stays crisp, as the 1.x pens drew it: half the border rounded up shows the parent (BorderSize 1: one row,
            // 3: two rows), then whole rows of border, and no row along the top and bottom edges is blended half and half (libgdiplus
            // fills the bottom band one row lower, so the rows are checked exactly at the top only)
            foreach (var (border, ring) in new[] { (1, 1), (3, 2) })
            {
                b.BorderSize = border;
                using var bmp = ui.Render(b);
                for (var i = 0; i < ring + border; i++)
                {
                    PaintingTests.AssertColor(i < ring ? Color.Lime : Color.Red, bmp.GetPixel(75, i), $"BorderSize {border}, top row {i}");
                }
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(75, ring + border + 1), $"BorderSize {border}, face");
                for (var i = 0; i < 6; i++)
                {
                    AssertNotBlended(bmp.GetPixel(75, i), $"BorderSize {border}, top row {i}");
                    AssertNotBlended(bmp.GetPixel(75, 39 - i), $"BorderSize {border}, bottom row {39 - i}");
                }
            }
            // no border: a one-pixel ring
            b.BorderSize = 0;
            using (var bmp = ui.Render(b))
            {
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(75, 0), "ring without border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(75, 2), "face without border");
            }
            // square corners (radius up to 2): the face and the border reach the edges, nothing of the parent shows
            b.BorderRadius = 2;
            b.BorderSize = 3;
            using (var bmp = ui.Render(b))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(1, 1), "square corner");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(75, 1), "square border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(75, 5), "square face");
                Assert.Equal(0, Gdi.Count(bmp, Gdi.IsLime));
            }
        });

        // The face keeps every state that the base class paints: the mouse-over color of the flat style shows inside the rounded
        // shape, the parent around it
        [Fact]
        public void MouseOverColor_PaintedInsideTheShape() => Sta.Run(ui =>
        {
            var b = OnLime(ui, new YANBtn());
            b.FlatAppearance.MouseOverBackColor = Color.Red;
            Priv.Call(b, "OnMouseEnter", EventArgs.Empty);
            using var bmp = ui.Render(b);
            PaintingTests.AssertColor(Color.Red, bmp.GetPixel(75, 20), "mouse-over face");
            PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(1, 1), "corner");
        });

        // BorderSize and BorderRadius are 96-dpi pixels: at 192 dpi the border and the ring are twice as wide
        [Fact]
        public void Border_ScalesWithTheDpi() => Sta.Run(ui =>
        {
            var b = OnLime(ui, new YANBtn { BorderRadius = 20, BorderSize = 2, BorderColor = Color.Red });
            using (var bmp = ui.Render(b))
            {
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(75, 2), "96 dpi border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(75, 4), "96 dpi face");
            }
            YANPaint.DpiOverride = 192;
            try
            {
                // rebuilt for the new DPI by itself (RescaleConstantsForDpi drops the cache too)
                using var bmp = ui.Render(b);
                PaintingTests.AssertColor(Color.Lime, bmp.GetPixel(75, 1), "192 dpi ring");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(75, 3), "192 dpi border");
                PaintingTests.AssertColor(Color.Red, bmp.GetPixel(75, 4), "192 dpi border");
                PaintingTests.AssertColor(Color.Blue, bmp.GetPixel(75, 8), "192 dpi face");
                Assert.Equal(2, b.BorderSize);
            }
            finally
            {
                YANPaint.DpiOverride = null;
            }
        });

        // High contrast: the border takes the system frame color (the configured color is not changed)
        [Fact]
        public void HighContrast_BorderInTheFrameColor() => Sta.Run(ui =>
        {
            var b = OnLime(ui, new YANBtn { BorderRadius = 0, BorderSize = 4, BorderColor = Color.Red });
            YANPaint.HighContrastOverride = true;
            try
            {
                using var bmp = ui.Render(b);
                PaintingTests.AssertColor(SystemColors.WindowFrame, bmp.GetPixel(75, 1), "border");
                Assert.Equal(Color.Red, b.BorderColor);
            }
            finally
            {
                YANPaint.HighContrastOverride = null;
            }
        });

        // 2.0 redeclares FlatStyle with the constructor's default, Flat: the designer writes Standard (1.x lost it) and no longer
        // writes Flat; the property keeps the framework's category and localizability
        [Fact]
        public void FlatStyle_DesignerDefaultIsFlat() => Sta.Run(() =>
        {
            using var b = new YANBtn();
            var prop = TypeDescriptor.GetProperties(b)[nameof(YANBtn.FlatStyle)];
            Assert.Equal(typeof(YANBtn), prop.ComponentType);
            Assert.Equal(FlatStyle.Flat, ((DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]).Value);
            Assert.True(((LocalizableAttribute)prop.Attributes[typeof(LocalizableAttribute)]).IsLocalizable);
            Assert.Equal(FlatStyle.Flat, b.FlatStyle);
            Assert.False(prop.ShouldSerializeValue(b));
            b.FlatStyle = FlatStyle.Standard;
            Assert.Equal(FlatStyle.Standard, ((ButtonBase)b).FlatStyle);
            Assert.True(prop.ShouldSerializeValue(b), "Standard must be written");
            prop.ResetValue(b);
            Assert.Equal(FlatStyle.Flat, b.FlatStyle);
            // code compiled against 1.x sets the inherited property: the same value
            ((ButtonBase)b).FlatStyle = FlatStyle.Popup;
            Assert.Equal(FlatStyle.Popup, b.FlatStyle);
        });

        // FlatAppearance.BorderSize gets the constructor's default, 0, through a type description provider of the button's
        // FlatAppearance: the designer writes 1 (1.x lost it), no longer writes 0, and Reset goes back to 0. The other FlatAppearance
        // properties are the framework's
        [Fact]
        public void FlatAppearanceBorderSize_DesignerDefaultIsZero() => Sta.Run(() =>
        {
            using var b = new YANBtn();
            var props = TypeDescriptor.GetProperties(b.FlatAppearance);
            var prop = props[nameof(FlatButtonAppearance.BorderSize)];
            Assert.Equal(0, ((DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]).Value);
            Assert.Equal(0, b.FlatAppearance.BorderSize);
            Assert.False(prop.ShouldSerializeValue(b.FlatAppearance));
            Assert.False(prop.CanResetValue(b.FlatAppearance));
            prop.SetValue(b.FlatAppearance, 1);
            Assert.Equal(1, b.FlatAppearance.BorderSize);
            Assert.True(prop.ShouldSerializeValue(b.FlatAppearance), "a border size of 1 must be written");
            Assert.True(prop.CanResetValue(b.FlatAppearance));
            prop.ResetValue(b.FlatAppearance);
            Assert.Equal(0, b.FlatAppearance.BorderSize);
            // the description, the category and the other properties are the framework's
            using var plain = new Button();
            var plainProp = TypeDescriptor.GetProperties(plain.FlatAppearance)[nameof(FlatButtonAppearance.BorderSize)];
            Assert.Equal(plainProp.Description, prop.Description);
            Assert.Equal(plainProp.Category, prop.Category);
            Assert.Equal(typeof(int), prop.PropertyType);
            var mouseOver = props[nameof(FlatButtonAppearance.MouseOverBackColor)];
            Assert.False(mouseOver.ShouldSerializeValue(b.FlatAppearance));
            b.FlatAppearance.MouseOverBackColor = Color.Red;
            Assert.True(mouseOver.ShouldSerializeValue(b.FlatAppearance));
            // a plain Button keeps the framework's default
            Assert.Equal(1, ((DefaultValueAttribute)plainProp.Attributes[typeof(DefaultValueAttribute)]).Value);
        });

        // The outlines are built once per size and DPI, not per paint
        [Fact]
        public void Outlines_BuiltOncePerSize() => Sta.Run(ui =>
        {
            var b = OnLime(ui, new YANBtn { BorderSize = 2 });
            ui.Draw(b);
            var path = Priv.Field<System.Drawing.Drawing2D.GraphicsPath>(b, "_pathSurface");
            Assert.NotNull(path);
            ui.Draw(b);
            Assert.Same(path, Priv.Field<System.Drawing.Drawing2D.GraphicsPath>(b, "_pathSurface"));
            b.Size = new Size(100, 40);
            ui.Draw(b);
            Assert.NotSame(path, Priv.Field<System.Drawing.Drawing2D.GraphicsPath>(b, "_pathSurface"));
            b.Dispose();
            Assert.Null(Priv.Field<System.Drawing.Drawing2D.GraphicsPath>(b, "_pathSurface"));
        });

        // A 150 x 40 blue button without text on a lime parent that is white underneath
        private static YANBtn OnLime(Ui ui, YANBtn b)
        {
            b.Text = "";
            b.BackColor = Color.Blue;
            b.Size = new Size(150, 40);
            AllControlsTests.OnParent(ui, new PaintingTests.LimePanel(), b, new Point(20, 20));
            return b;
        }

        // Checks that a pixel is the parent (lime), the border (red) or the face (blue), not a blend of two of them
        private static void AssertNotBlended(Color pixel, string what)
        {
            const int TOLERANCE = 8;
            bool Near(Color c) => Math.Abs(c.R - pixel.R) <= TOLERANCE && Math.Abs(c.G - pixel.G) <= TOLERANCE && Math.Abs(c.B - pixel.B) <= TOLERANCE;
            Assert.True(Near(Color.Lime) || Near(Color.Red) || Near(Color.Blue), $"{what}: {pixel} is a blend");
        }

        private static YANBtn Colored(YANBtn b)
        {
            b.Text = "";
            b.ForeColor = Color.Lime;
            b.BackColor = Color.Blue;
            b.Size = new Size(150, 40);
            return b;
        }

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

        private static bool GetStyle(Control c, ControlStyles style) => (bool)Priv.Call(c, "GetStyle", style);

        // Every delegate subscribed to one of the parent's BackColor events (event keys looked up by name)
        private static List<Delegate> BackColorHandlers(Control parent)
        {
            var events = Priv.Property<EventHandlerList>(parent, "Events");
            var keys = typeof(Control).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(object) && f.Name.IndexOf("BackColor", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(f => f.GetValue(null))
                .ToList();
            Assert.NotEmpty(keys);
            return keys.Select(k => events[k]).Where(d => d != null).SelectMany(d => d.GetInvocationList()).ToList();
        }
    }
}
