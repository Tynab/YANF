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

        // 1.0.2 assigned a new Region in OnPaint, and assigning a region invalidates the window: an endless repaint loop
        [Fact]
        public void Region_OnlyChangesWithShape() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = 20, BorderSize = 2, Size = new Size(150, 40) };
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
        });

        // The hand cursor comes from DefaultCursor now; 1.0.2 overwrote Cursor on every mouse move
        [Fact]
        public void UserCursor_NotOverwrittenOnMouseMove() => Sta.Run(() =>
        {
            using var b = new YANBtn { Cursor = Cursors.Cross };
            Priv.Call(b, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));
            Assert.Same(Cursors.Cross, b.Cursor);
        });

        // 1.0 forced TabStop = false and TabIndex = 0; the button still does not take the focus unless Focusable is set
        [Fact]
        public void NewButton_StandardTabStop_NotFocusable() => Sta.Run(ui =>
        {
            var b1 = new YANBtn();
            var b2 = new YANBtn();
            Assert.True(b1.TabStop);
            Assert.False(b1.Focusable);
            Assert.False(GetStyle(b1, ControlStyles.Selectable));
            Assert.False(TypeDescriptor.GetProperties(b1)[nameof(YANBtn.Focusable)].ShouldSerializeValue(b1), "Focusable must not be written at its default");
            // code-created buttons get the next tab index, like any control (1.0 gave all of them 0)
            var pnl = new Panel();
            ui.Host.Controls.Add(pnl);
            pnl.Controls.Add(b1);
            pnl.Controls.Add(b2);
            Assert.Equal((0, 1), (b1.TabIndex, b2.TabIndex));
            ui.Show(new TextBox(), b1);
            Assert.False(b1.CanSelect);
        });

        [Fact]
        public void Focusable_WrapsSelectable() => Sta.Run(ui =>
        {
            var b = new YANBtn();
            ui.Show(new TextBox(), b);
            b.Focusable = true;
            Assert.True(b.Focusable);
            Assert.True(GetStyle(b, ControlStyles.Selectable));
            Assert.True(b.CanSelect);
            Assert.True(TypeDescriptor.GetProperties(b)[nameof(YANBtn.Focusable)].ShouldSerializeValue(b));
            b.Focusable = false;
            Assert.False(GetStyle(b, ControlStyles.Selectable));
            Assert.False(b.CanSelect);
        });

        // TAB reaches a button when it is Focusable and its TabStop is true (1.0 designer files set TabStop = false)
        [Fact]
        public void Tab_ReachesFocusableButtonsOnly() => Sta.Run(ui =>
        {
            var txt = new TextBox();
            var plain = new YANBtn();
            var legacy = new YANBtn { Focusable = true, TabStop = false };
            var focusable = new YANBtn { Focusable = true };
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
            var b = new YANBtn();
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
            var b = new YANBtn { Text = "&Save" };
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

        // The flat style's own focus rectangle, which the rounded region would cut off, is not drawn: ShowFocusCues reads false
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
