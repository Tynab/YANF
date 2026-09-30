using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using YANF.Script;
using static System.Windows.Forms.DialogResult;
using static YANF.Script.YANConstant;

namespace YANF.Tests.Script
{
    using Control = System.Windows.Forms.Control;

    // YANMessageBox (1.1): the 20 overloads forward to one options path, the three copies of the button code became one table and
    // one layout loop that keeps the 1.0 pixels, Enter/Esc/✕ follow the Windows message box, and the fonts are created once per box
    public class YANMessageBoxTests
    {
        #region Fields
        private const string TEXT = "Do you want to continue?";
        private static readonly string[] PARTS = { "pnlHeader", "btnClose", "lblCaption", "pnlFooter", "btn1", "btn2", "btn3", "pnlBody", "lblMessage", "picIcon" };
        #endregion

        #region Layout
        // A short text without a caption gives the narrowest box, so the 1.0 pixels are known exactly: the box is n*100 + (n+1)*10 + 4
        // wide and the 100x35 buttons sit 10 px from the top of the footer, 20 px apart in pairs and 10 px apart in threes
        [Theory]
        [InlineData(MessageBoxButtons.OK, 124, "10")]
        [InlineData(MessageBoxButtons.OKCancel, 234, "5|125")]
        [InlineData(MessageBoxButtons.AbortRetryIgnore, 344, "10|120|230")]
        [InlineData(MessageBoxButtons.YesNoCancel, 344, "10|120|230")]
        [InlineData(MessageBoxButtons.YesNo, 234, "5|125")]
        [InlineData(MessageBoxButtons.RetryCancel, 234, "5|125")]
        public void Layout_EveryButtonSet_KeepsTheOriginalPixels(MessageBoxButtons btns, int width, string lefts) => Sta.Run(() =>
        {
            var xs = lefts.Split('|').Select(int.Parse).ToArray();
            var (results, colors) = Expected(btns);
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = "Hi", Buttons = btns });
            Assert.Equal(width, frm.Width);
            ShowModal(frm, box =>
            {
                var footer = Priv.Field<Panel>(box, "pnlFooter");
                Assert.Equal(width - 4, footer.Width);
                Assert.Equal(35 + Priv.Field<Label>(box, "lblMessage").Height + 55 + 10 + 4, box.Height);
                var slots = Slots(box);
                for (var i = 0; i < 3; i++)
                {
                    Assert.Equal(i < xs.Length, slots[i].Visible);
                    if (i < xs.Length)
                    {
                        Assert.Equal(new Rectangle(xs[i], 10, 100, 35), slots[i].Bounds);
                        Assert.Equal(results[i], slots[i].DialogResult);
                        Assert.Equal(colors[i].ToArgb(), slots[i].BackColor.ToArgb());
                        Assert.Equal(Color.White.ToArgb(), slots[i].ForeColor.ToArgb());
                        Assert.Equal(results[i].ToString(), slots[i].Text);
                    }
                }
                Slots(box)[0].PerformClick();
            });
        });

        // A box wider than its buttons (long caption) centers them with the 1.0 arithmetic
        [Theory]
        [InlineData(MessageBoxButtons.OK)]
        [InlineData(MessageBoxButtons.OKCancel)]
        [InlineData(MessageBoxButtons.YesNoCancel)]
        public void Layout_WideBox_CentersTheButtonsAsBefore(MessageBoxButtons btns) => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Caption = "A much longer caption text for the header bar of the box", Text = "Hi", Buttons = btns });
            var n = Expected(btns).Results.Length;
            Assert.True(frm.Width > n * 100 + (n + 1) * 10 + 4, "the caption should widen the box: " + frm.Width);
            ShowModal(frm, box =>
            {
                var xCtr = (Priv.Field<Panel>(box, "pnlFooter").Width - 100) / 2;
                var xs = n switch
                {
                    1 => new[] { xCtr },
                    2 => new[] { xCtr - 100 / 2 - 10, xCtr + 100 / 2 + 10 },
                    _ => new[] { xCtr - 100 - 10, xCtr, xCtr + 100 + 10 }
                };
                Assert.Equal(xs, Slots(box).Take(n).Select(b => b.Left));
                Assert.All(Slots(box).Take(n), b => Assert.Equal(10, b.Top));
                Slots(box)[0].PerformClick();
            });
        });

        // Long text wraps at half of the working area (less the label's 20 px side padding) instead of making the box wider than the
        // screen. The multi-line layout then drops that padding and narrows MaximumSize by as much, so the text keeps its line breaks
        // and still fits the box that was sized for it (wrapping again at the full width could push a line past the right edge)
        [Fact]
        public void Layout_LongText_WrapsAtHalfTheWorkingArea() => Sta.Run(() =>
        {
            var text = string.Join(" ", Enumerable.Repeat("lorem ipsum dolor sit amet", 80));
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = text });
            var max = SystemInformation.WorkingArea.Width / 2;
            var lbl = Priv.Field<Label>(frm, "lblMessage");
            Assert.True(lbl.Height > 2 * lbl.Font.Height, $"the long text should wrap: label {lbl.Size}");
            Assert.Equal(new Padding(0, 0, 0, 15), lbl.Padding);
            Assert.Equal(new Size(max - 20, 0), lbl.MaximumSize);
            Assert.InRange(lbl.Width, 1, max - 20);
            Assert.InRange(frm.Width, 1, max + 50 + 10);
            ShowModal(frm, box =>
            {
                var body = Priv.Field<Panel>(box, "pnlBody");
                Assert.Equal(60, lbl.Left);
                Assert.True(lbl.Right - lbl.Padding.Right <= body.ClientSize.Width, $"the text is cut off: label {lbl.Bounds}, body {body.ClientSize}");
                Assert.Equal(35 + lbl.Height + 55 + 10 + 4, box.Height);
                Slots(box)[0].PerformClick();
            });
        });

        // Explicit line breaks keep the 1.0 layout: the label loses its side padding and the box keeps the width of the padded text
        [Fact]
        public void Layout_MultiLineText_KeepsTheOriginalLayout() => Sta.Run(() =>
        {
            using var one = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = "A first line of text" });
            using var two = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = "A first line of text\nsecond" });
            var lbl1 = Priv.Field<Label>(one, "lblMessage");
            var lbl2 = Priv.Field<Label>(two, "lblMessage");
            // (a font taller than 17 px, as on mono, already counts one line as multi-line, hence lbl1's own padding)
            Assert.Equal(new Padding(0, 0, 0, 15), lbl2.Padding);
            Assert.Equal(lbl1.Width - lbl1.Padding.Horizontal, lbl2.Width);
            Assert.Equal(lbl2.Width + 20 + 60, two.Width);
            Assert.Equal(one.Width, two.Width);
        });
        #endregion

        #region Default, Enter, Esc and ✕
        // The default button is one the box shows (1.0 focused the hidden btn2/btn3 of an OK box, and already fell back to btn1 for Button3
        // in a two-button box); it is the AcceptButton and the active control and is marked with a border, not an underlined font
        [Theory]
        [InlineData(MessageBoxButtons.OK, MessageBoxDefaultButton.Button1, 0)]
        [InlineData(MessageBoxButtons.OK, MessageBoxDefaultButton.Button2, 0)]
        [InlineData(MessageBoxButtons.OK, MessageBoxDefaultButton.Button3, 0)]
        [InlineData(MessageBoxButtons.OKCancel, MessageBoxDefaultButton.Button2, 1)]
        [InlineData(MessageBoxButtons.OKCancel, MessageBoxDefaultButton.Button3, 0)]
        [InlineData(MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2, 1)]
        [InlineData(MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button3, 0)]
        [InlineData(MessageBoxButtons.RetryCancel, MessageBoxDefaultButton.Button3, 0)]
        [InlineData(MessageBoxButtons.YesNoCancel, MessageBoxDefaultButton.Button1, 0)]
        [InlineData(MessageBoxButtons.YesNoCancel, MessageBoxDefaultButton.Button2, 1)]
        [InlineData(MessageBoxButtons.YesNoCancel, MessageBoxDefaultButton.Button3, 2)]
        [InlineData(MessageBoxButtons.AbortRetryIgnore, MessageBoxDefaultButton.Button3, 2)]
        [InlineData(MessageBoxButtons.AbortRetryIgnore, (MessageBoxDefaultButton)0x300, 0)]
        public void DefaultButton_IsAVisibleButton_AndEnterPressesIt(MessageBoxButtons btns, MessageBoxDefaultButton dflt, int expected) => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT, Buttons = btns, DefaultButton = dflt });
            var slots = Slots(frm);
            Assert.Same(slots[expected], frm.AcceptButton);
            Assert.Same(slots[expected], frm.ActiveControl);
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(i == expected ? 2 : 0, slots[i].FlatAppearance.BorderSize);
                Assert.False(slots[i].Font.Underline);
            }
            Assert.Equal(Color.White.ToArgb(), slots[expected].FlatAppearance.BorderColor.ToArgb());
            var res = ShowModal(frm, box =>
            {
                Assert.True(slots[expected].Visible);
                Assert.Same(slots[expected], box.ActiveControl);
                Assert.Equal(true, Priv.Call(box, "ProcessDialogKey", Keys.Enter));
            });
            Assert.Equal(Expected(btns).Results[expected], res);
        });

        // Enter presses the focused button (Form.UpdateDefaultButton), so the border follows the focus from button to button
        [Fact]
        public void DefaultMark_FollowsTheFocus() => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT, Buttons = MessageBoxButtons.YesNoCancel });
            var slots = Slots(frm);
            var res = ShowModal(frm, box =>
            {
                Assert.Equal(new[] { 2, 0, 0 }, slots.Select(b => b.FlatAppearance.BorderSize));
                Assert.True(slots[2].Focus(), "btn3 did not take the focus");
                Assert.Equal(new[] { 0, 0, 2 }, slots.Select(b => b.FlatAppearance.BorderSize));
                Assert.Equal(Color.White.ToArgb(), slots[2].FlatAppearance.BorderColor.ToArgb());
                Assert.True(slots[1].Focus(), "btn2 did not take the focus");
                Assert.Equal(new[] { 0, 2, 0 }, slots.Select(b => b.FlatAppearance.BorderSize));
                Assert.Same(slots[0], box.AcceptButton);
                Assert.Equal(true, Priv.Call(box, "ProcessDialogKey", Keys.Enter));
            });
            Assert.Equal(No, res);
        });

        // Esc presses the Cancel button, or OK in an OK-only box; Yes/No and Abort/Retry/Ignore have no Esc button (Windows rule)
        [Theory]
        [InlineData(MessageBoxButtons.OK, 0)]
        [InlineData(MessageBoxButtons.OKCancel, 1)]
        [InlineData(MessageBoxButtons.AbortRetryIgnore, -1)]
        [InlineData(MessageBoxButtons.YesNoCancel, 2)]
        [InlineData(MessageBoxButtons.YesNo, -1)]
        [InlineData(MessageBoxButtons.RetryCancel, 1)]
        public void CancelButton_FollowsTheWindowsRule(MessageBoxButtons btns, int expected) => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT, Buttons = btns });
            Assert.Same(expected < 0 ? null : Slots(frm)[expected], frm.CancelButton);
            var res = ShowModal(frm, box =>
            {
                var handled = (bool)Priv.Call(box, "ProcessDialogKey", Keys.Escape);
                Assert.Equal(expected >= 0, handled);
                if (!handled)
                {
                    Assert.True(box.Visible);
                    Slots(box)[1].PerformClick();
                }
            });
            Assert.Equal(expected >= 0 ? Expected(btns).Results[expected] : Expected(btns).Results[1], res);
        });

        // ✕ gives Cancel for every set as in 1.0; with StrictClose it gives the Esc button's result and is hidden when there is none
        [Theory]
        [InlineData(MessageBoxButtons.OK, false, Cancel)]
        [InlineData(MessageBoxButtons.YesNo, false, Cancel)]
        [InlineData(MessageBoxButtons.AbortRetryIgnore, false, Cancel)]
        [InlineData(MessageBoxButtons.YesNoCancel, false, Cancel)]
        [InlineData(MessageBoxButtons.OK, true, DialogResult.OK)]
        [InlineData(MessageBoxButtons.OKCancel, true, Cancel)]
        [InlineData(MessageBoxButtons.RetryCancel, true, Cancel)]
        [InlineData(MessageBoxButtons.YesNoCancel, true, Cancel)]
        [InlineData(MessageBoxButtons.YesNo, true, None)]
        [InlineData(MessageBoxButtons.AbortRetryIgnore, true, None)]
        public void CloseBox_CancelByDefault_WindowsRuleWhenStrict(MessageBoxButtons btns, bool isStrict, DialogResult expected) => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT, Buttons = btns, StrictClose = isStrict });
            var res = ShowModal(frm, box =>
            {
                var close = Priv.Field<Button>(box, "btnClose");
                Assert.Equal(expected != None, close.Visible);
                if (close.Visible)
                {
                    close.PerformClick();
                }
                else
                {
                    Slots(box)[0].PerformClick();
                }
            });
            Assert.Equal(expected != None ? expected : Expected(btns).Results[0], res);
        });
        #endregion

        #region Languages and fonts
        // The one text table: VIE and JAP keep the 1.0 wording (VIE's OK-only button is "Đóng", "Xong" next to Cancel); ENG and no
        // language give the English texts with the designer fonts
        [Theory]
        [InlineData(null, MessageBoxButtons.OK, "OK")]
        [InlineData(null, MessageBoxButtons.OKCancel, "OK|Cancel")]
        [InlineData(null, MessageBoxButtons.AbortRetryIgnore, "Abort|Retry|Ignore")]
        [InlineData(null, MessageBoxButtons.YesNoCancel, "Yes|No|Cancel")]
        [InlineData(null, MessageBoxButtons.YesNo, "Yes|No")]
        [InlineData(null, MessageBoxButtons.RetryCancel, "Retry|Cancel")]
        [InlineData("ENG", MessageBoxButtons.OK, "OK")]
        [InlineData("ENG", MessageBoxButtons.OKCancel, "OK|Cancel")]
        [InlineData("ENG", MessageBoxButtons.AbortRetryIgnore, "Abort|Retry|Ignore")]
        [InlineData("ENG", MessageBoxButtons.YesNoCancel, "Yes|No|Cancel")]
        [InlineData("ENG", MessageBoxButtons.YesNo, "Yes|No")]
        [InlineData("ENG", MessageBoxButtons.RetryCancel, "Retry|Cancel")]
        [InlineData("VIE", MessageBoxButtons.OK, "Đóng")]
        [InlineData("VIE", MessageBoxButtons.OKCancel, "Xong|Hủy")]
        [InlineData("VIE", MessageBoxButtons.AbortRetryIgnore, "Hủy Bỏ|Thử lại|Bỏ qua")]
        [InlineData("VIE", MessageBoxButtons.YesNoCancel, "Vâng|Không|Hủy")]
        [InlineData("VIE", MessageBoxButtons.YesNo, "Vâng|Không")]
        [InlineData("VIE", MessageBoxButtons.RetryCancel, "Thử lại|Hủy")]
        [InlineData("JAP", MessageBoxButtons.OK, "オーケー")]
        [InlineData("JAP", MessageBoxButtons.OKCancel, "オーケー|キャンセル")]
        [InlineData("JAP", MessageBoxButtons.AbortRetryIgnore, "アボート|リトライ|無視")]
        [InlineData("JAP", MessageBoxButtons.YesNoCancel, "はい|いいえ|キャンセル")]
        [InlineData("JAP", MessageBoxButtons.YesNo, "はい|いいえ")]
        [InlineData("JAP", MessageBoxButtons.RetryCancel, "リトライ|キャンセル")]
        public void Language_ButtonTexts(string lang, MessageBoxButtons btns, string texts) => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT, Buttons = btns, Language = Lang(lang) });
            var expected = texts.Split('|');
            Assert.Equal(expected, Slots(frm).Take(expected.Length).Select(b => b.Text));
            // the table itself, for every result of the set
            var results = Expected(btns).Results;
            for (var i = 0; i < results.Length; i++)
            {
                Assert.Equal(expected[i], GetMsgBoxBtnText(Lang(lang) ?? MsgBoxLang.ENG, btns, results[i]));
            }
        });

        // VIE and JAP get their 1.0 fonts (caption, message, buttons); ENG and no language allocate no font at all
        [Theory]
        [InlineData(null, 12f, 10f, 10f)]
        [InlineData("ENG", 12f, 10f, 10f)]
        [InlineData("VIE", 10f, 9.5f, 10f)]
        [InlineData("JAP", 12f, 8f, 9f)]
        public void Language_Fonts(string lang, float caption, float message, float buttons) => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Caption = "Cap", Text = TEXT, Buttons = MessageBoxButtons.YesNoCancel, Language = Lang(lang) });
            Assert.Equal(caption, Priv.Field<Label>(frm, "lblCaption").Font.SizeInPoints, 2);
            Assert.Equal(message, Priv.Field<Label>(frm, "lblMessage").Font.SizeInPoints, 2);
            var slots = Slots(frm);
            Assert.All(slots, b => Assert.Equal(buttons, b.Font.SizeInPoints, 2));
            Assert.All(slots, b => Assert.Equal(FontStyle.Regular, b.Font.Style));
            // one font shared by the three buttons (1.0 made an underlined copy for the default button)
            Assert.Same(slots[0].Font, slots[1].Font);
            Assert.Same(slots[0].Font, slots[2].Font);
            if (Lang(lang) is null or MsgBoxLang.ENG)
            {
                Assert.Same(frm.Font, slots[0].Font);
                Assert.Same(frm.Font, Priv.Field<Label>(frm, "lblMessage").Font);
            }
        });

        // The fonts and the icon a box creates are disposed with it; the designer's fonts are not
        [Theory]
        [InlineData("VIE")]
        [InlineData("JAP")]
        public void Dispose_DisposesTheBoxOwnFontsAndIcon(string lang) => Sta.Run(() =>
        {
            var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Caption = "Cap", Text = TEXT, Icon = MessageBoxIcon.Error, Language = Lang(lang) });
            var own = new[] { Priv.Field<Label>(frm, "lblCaption").Font, Priv.Field<Label>(frm, "lblMessage").Font, Slots(frm)[0].Font };
            var designer = new[] { frm.Font, Priv.Field<Button>(frm, "btnClose").Font, Priv.Field<Panel>(frm, "pnlHeader").Font };
            var icon = Priv.Field<PictureBox>(frm, "picIcon").Image;
            Assert.All(own, f => Assert.DoesNotContain(designer, d => ReferenceEquals(d, f)));
            frm.Dispose();
            Assert.All(own, f => Assert.True(Gdi.IsDisposed(f), "language font not disposed"));
            Assert.All(designer, f => Assert.False(Gdi.IsDisposed(f), "designer font disposed"));
            Assert.True(Gdi.IsDisposed(icon), "icon not disposed");
        });
        #endregion

        #region Options and the 1.0 overloads
        [Fact]
        public void Options_Defaults()
        {
            var o = new YANMessageBoxOptions();
            Assert.Null(o.Caption);
            Assert.Null(o.Text);
            Assert.Equal(MessageBoxButtons.OK, o.Buttons);
            Assert.Equal(MessageBoxIcon.None, o.Icon);
            Assert.Equal(MessageBoxDefaultButton.Button1, o.DefaultButton);
            Assert.Null(o.Language);
            Assert.True(o.TopMost);
            Assert.False(o.StrictClose);
        }

        // Each of the 20 1.0 overloads shows the same box and returns the same result as the options it forwards to
        [Fact]
        public void Show_LegacyOverloads_EqualTheOptionsPath() => Sta.Run(ui =>
        {
            var owner = ui.Show();
            foreach (var (name, call, options) in Overloads())
            {
                ui.Case(name, () =>
                {
                    var legacy = ShowAndPress(() => call(owner), box => box.AcceptButton);
                    var viaOptions = ShowAndPress(() => name.StartsWith("(owner") ? YANMessageBox.Show(owner, options) : YANMessageBox.Show(options), box => box.AcceptButton);
                    Assert.Equal(viaOptions.Snapshot, legacy.Snapshot);
                    Assert.Equal(viaOptions.Result, legacy.Result);
                });
            }
        });

        // Each of the 10 1.0 constructors builds the same box as the options it chains to
        [Fact]
        public void Screen_LegacyConstructors_EqualTheOptionsConstructor() => Sta.Run(ui =>
        {
            const string cap = "Cap";
            const MessageBoxButtons btns = MessageBoxButtons.AbortRetryIgnore;
            const MessageBoxIcon icon = MessageBoxIcon.Error;
            const MessageBoxDefaultButton dflt = MessageBoxDefaultButton.Button3;
            foreach (var lang in new[] { MsgBoxLang.VIE, MsgBoxLang.JAP, MsgBoxLang.ENG })
            {
                var cases = new (string Name, Func<YANMessageBoxScreen> Legacy, YANMessageBoxOptions Options)[]
                {
                    ("(text)", () => new YANMessageBoxScreen(TEXT), new YANMessageBoxOptions { Text = TEXT }),
                    ("(text, lang)", () => new YANMessageBoxScreen(TEXT, lang), new YANMessageBoxOptions { Text = TEXT, Language = lang }),
                    ("(cap, text)", () => new YANMessageBoxScreen(cap, TEXT), new YANMessageBoxOptions { Caption = cap, Text = TEXT }),
                    ("(cap, text, lang)", () => new YANMessageBoxScreen(cap, TEXT, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Language = lang }),
                    ("(cap, text, btns)", () => new YANMessageBoxScreen(cap, TEXT, btns), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns }),
                    ("(cap, text, btns, lang)", () => new YANMessageBoxScreen(cap, TEXT, btns, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Language = lang }),
                    ("(cap, text, btns, icon)", () => new YANMessageBoxScreen(cap, TEXT, btns, icon), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon }),
                    ("(cap, text, btns, icon, lang)", () => new YANMessageBoxScreen(cap, TEXT, btns, icon, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, Language = lang }),
                    ("(cap, text, btns, icon, dflt)", () => new YANMessageBoxScreen(cap, TEXT, btns, icon, dflt), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, DefaultButton = dflt }),
                    ("(cap, text, btns, icon, dflt, lang)", () => new YANMessageBoxScreen(cap, TEXT, btns, icon, dflt, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, DefaultButton = dflt, Language = lang })
                };
                foreach (var (name, legacy, options) in cases)
                {
                    ui.Case($"{name} {lang}", () =>
                    {
                        using var a = legacy();
                        using var b = new YANMessageBoxScreen(options);
                        Assert.Equal(Snapshot(b), Snapshot(a));
                    });
                }
            }
            // ENG renders exactly like no language
            using var eng = new YANMessageBoxScreen(new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Language = MsgBoxLang.ENG });
            using var none = new YANMessageBoxScreen(new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns });
            Assert.Equal(Snapshot(none), Snapshot(eng));
        });

        // Icon: image and accent color as in 1.0 (the Error box also reddens the close box's hover color)
        [Theory]
        [InlineData(MessageBoxIcon.None, 100, 149, 237)]
        [InlineData(MessageBoxIcon.Error, 224, 79, 95)]
        [InlineData(MessageBoxIcon.Information, 38, 191, 166)]
        [InlineData(MessageBoxIcon.Question, 10, 119, 232)]
        [InlineData(MessageBoxIcon.Warning, 255, 140, 0)]
        public void Icon_SetsTheAccentColor(MessageBoxIcon icon, int r, int g, int b) => Sta.Run(() =>
        {
            using var frm = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT, Icon = icon });
            Assert.Equal(Color.FromArgb(r, g, b).ToArgb(), frm.PrimaryColor.ToArgb());
            Assert.Equal(Color.FromArgb(r, g, b).ToArgb(), frm.BackColor.ToArgb());
            Assert.NotNull(Priv.Field<PictureBox>(frm, "picIcon").Image);
            var hover = Priv.Field<Button>(frm, "btnClose").FlatAppearance.MouseOverBackColor;
            Assert.Equal((icon == MessageBoxIcon.Error ? Color.Crimson : Color.FromArgb(224, 79, 95)).ToArgb(), hover.ToArgb());
        });

        [Fact]
        public void TopMost_FollowsTheOptions() => Sta.Run(() =>
        {
            using var dflt = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT });
            Assert.True(dflt.TopMost);
            using var off = new YANMessageBoxScreen(new YANMessageBoxOptions { Text = TEXT, TopMost = false });
            Assert.False(off.TopMost);
        });

        [Fact]
        public void InvalidArguments_Throw() => Sta.Run(() =>
        {
            Assert.Throws<ArgumentNullException>(() => YANMessageBox.Show((YANMessageBoxOptions)null));
            Assert.Throws<ArgumentNullException>(() => YANMessageBox.Show((IWin32Window)null, (YANMessageBoxOptions)null));
            Assert.Throws<ArgumentNullException>(() => new YANMessageBoxScreen((YANMessageBoxOptions)null));
            // an unknown button set threw nothing in 1.0 and showed a box without buttons
            Assert.Throws<InvalidEnumArgumentException>(() => new YANMessageBoxScreen(new YANMessageBoxOptions { Buttons = (MessageBoxButtons)6 }));
            Assert.Throws<InvalidEnumArgumentException>(() => YANMessageBox.Show("Cap", TEXT, (MessageBoxButtons)99));
            Assert.DoesNotContain(Application.OpenForms.OfType<YANMessageBoxScreen>(), f => !f.IsDisposed && f.Visible);
        });

        // Called from a worker thread with an owner, the box is shown on the owner's UI thread (1.0 showed it on the worker)
        [Fact]
        public void Show_FromAWorkerThread_MarshalsToTheOwnersThread() => Sta.Run(ui =>
        {
            var owner = ui.Show();
            var calls = new (string Name, Func<DialogResult> Call)[]
            {
                ("options", () => YANMessageBox.Show(owner, new YANMessageBoxOptions { Caption = "Cap", Text = TEXT, Buttons = MessageBoxButtons.YesNo })),
                ("1.0 overload", () => YANMessageBox.Show(owner, "Cap", TEXT, MessageBoxButtons.YesNo))
            };
            foreach (var (name, call) in calls)
            {
                ui.Case(name, () =>
                {
                    var uiThread = Thread.CurrentThread.ManagedThreadId;
                    var boxThread = -1;
                    // the watcher runs on this (the owner's) thread and only sees boxes whose handle belongs to it
                    using var watcher = new Watcher(box =>
                    {
                        boxThread = Thread.CurrentThread.ManagedThreadId;
                        Assert.False(box.InvokeRequired);
                        Slots(box)[1].PerformClick();
                    });
                    DialogResult? result = null;
                    Exception error = null;
                    var worker = new Thread(() =>
                    {
                        try
                        {
                            result = call();
                        }
                        catch (Exception ex)
                        {
                            error = ex;
                        }
                    })
                    {
                        IsBackground = true
                    };
                    worker.Start();
                    var sw = Stopwatch.StartNew();
                    while (!worker.Join(10) && sw.ElapsedMilliseconds < Poll.TIMEOUT_MS)
                    {
                        Application.DoEvents();
                    }
                    Assert.False(worker.IsAlive, "the worker's Show call did not return");
                    watcher.Check();
                    if (error != null)
                    {
                        ExceptionDispatchInfo.Capture(error).Throw();
                    }
                    Assert.Equal(uiThread, boxThread);
                    Assert.Equal(No, result);
                });
            }
        });
        #endregion

        #region Metadata
        [Fact]
        public void Metadata_AdditiveApi()
        {
            Assert.True(typeof(YANMessageBox).IsAbstract && !typeof(YANMessageBox).IsSealed, "YANMessageBox stays abstract (not static) in 1.x");
            Assert.NotNull(typeof(YANMessageBox).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
            var shows = typeof(YANMessageBox).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == nameof(YANMessageBox.Show)).ToArray();
            Assert.Equal(22, shows.Length);
            Assert.All(shows, m => Assert.Null(m.GetCustomAttribute<EditorBrowsableAttribute>()));
            Assert.All(shows.SelectMany(m => m.GetParameters()), p => Assert.False(p.IsOptional, "no optional parameters"));
            // the 1.0 constructors of the screen are hidden in favor of the options
            var ctors = typeof(YANMessageBoxScreen).GetConstructors();
            Assert.Equal(11, ctors.Length);
            foreach (var c in ctors)
            {
                var ps = c.GetParameters();
                var isOptions = ps.Length == 1 && ps[0].ParameterType == typeof(YANMessageBoxOptions);
                var hidden = c.GetCustomAttribute<EditorBrowsableAttribute>()?.State == EditorBrowsableState.Never;
                Assert.Equal(!isOptions, hidden);
            }
            // MsgBoxLang: ENG appended, VIE and JAP keep their values
            Assert.Equal(0, (int)MsgBoxLang.VIE);
            Assert.Equal(1, (int)MsgBoxLang.JAP);
            Assert.Equal(2, (int)MsgBoxLang.ENG);
            Assert.NotNull(typeof(AnimateWindowFlags).GetCustomAttribute<FlagsAttribute>());
            Assert.Equal("AW_ACTIVATE, AW_SLIDE", (AnimateWindowFlags.AW_ACTIVATE | AnimateWindowFlags.AW_SLIDE).ToString());
        }
        #endregion

        #region Helpers
        // The 20 1.0 overloads with the options each one stands for (owner overloads start with "(owner")
        private static IEnumerable<(string Name, Func<IWin32Window, DialogResult> Call, YANMessageBoxOptions Options)> Overloads()
        {
            const string cap = "Cap";
            const MessageBoxButtons btns = MessageBoxButtons.YesNoCancel;
            const MessageBoxIcon icon = MessageBoxIcon.Warning;
            const MessageBoxDefaultButton dflt = MessageBoxDefaultButton.Button2;
            const MsgBoxLang lang = MsgBoxLang.JAP;
            yield return ("(text)", _ => YANMessageBox.Show(TEXT), new YANMessageBoxOptions { Text = TEXT });
            yield return ("(text, lang)", _ => YANMessageBox.Show(TEXT, lang), new YANMessageBoxOptions { Text = TEXT, Language = lang });
            yield return ("(cap, text)", _ => YANMessageBox.Show(cap, TEXT), new YANMessageBoxOptions { Caption = cap, Text = TEXT });
            yield return ("(cap, text, lang)", _ => YANMessageBox.Show(cap, TEXT, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Language = lang });
            yield return ("(cap, text, btns)", _ => YANMessageBox.Show(cap, TEXT, btns), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns });
            yield return ("(cap, text, btns, lang)", _ => YANMessageBox.Show(cap, TEXT, btns, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Language = lang });
            yield return ("(cap, text, btns, icon)", _ => YANMessageBox.Show(cap, TEXT, btns, icon), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon });
            yield return ("(cap, text, btns, icon, lang)", _ => YANMessageBox.Show(cap, TEXT, btns, icon, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, Language = lang });
            yield return ("(cap, text, btns, icon, dflt)", _ => YANMessageBox.Show(cap, TEXT, btns, icon, dflt), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, DefaultButton = dflt });
            yield return ("(cap, text, btns, icon, dflt, lang)", _ => YANMessageBox.Show(cap, TEXT, btns, icon, dflt, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, DefaultButton = dflt, Language = lang });
            yield return ("(owner, text)", o => YANMessageBox.Show(o, TEXT), new YANMessageBoxOptions { Text = TEXT });
            yield return ("(owner, text, lang)", o => YANMessageBox.Show(o, TEXT, lang), new YANMessageBoxOptions { Text = TEXT, Language = lang });
            yield return ("(owner, cap, text)", o => YANMessageBox.Show(o, cap, TEXT), new YANMessageBoxOptions { Caption = cap, Text = TEXT });
            yield return ("(owner, cap, text, lang)", o => YANMessageBox.Show(o, cap, TEXT, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Language = lang });
            yield return ("(owner, cap, text, btns)", o => YANMessageBox.Show(o, cap, TEXT, btns), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns });
            yield return ("(owner, cap, text, btns, lang)", o => YANMessageBox.Show(o, cap, TEXT, btns, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Language = lang });
            yield return ("(owner, cap, text, btns, icon)", o => YANMessageBox.Show(o, cap, TEXT, btns, icon), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon });
            yield return ("(owner, cap, text, btns, icon, lang)", o => YANMessageBox.Show(o, cap, TEXT, btns, icon, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, Language = lang });
            yield return ("(owner, cap, text, btns, icon, dflt)", o => YANMessageBox.Show(o, cap, TEXT, btns, icon, dflt), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, DefaultButton = dflt });
            yield return ("(owner, cap, text, btns, icon, dflt, lang)", o => YANMessageBox.Show(o, cap, TEXT, btns, icon, dflt, lang), new YANMessageBoxOptions { Caption = cap, Text = TEXT, Buttons = btns, Icon = icon, DefaultButton = dflt, Language = lang });
        }

        // The results and 1.0 colors of a button set, left to right
        private static (DialogResult[] Results, Color[] Colors) Expected(MessageBoxButtons btns) => btns switch
        {
            MessageBoxButtons.OK => (new[] { DialogResult.OK }, new[] { Color.SeaGreen }),
            MessageBoxButtons.OKCancel => (new[] { DialogResult.OK, Cancel }, new[] { Color.SeaGreen, Color.DimGray }),
            MessageBoxButtons.AbortRetryIgnore => (new[] { Abort, Retry, Ignore }, new[] { Color.Goldenrod, Color.SeaGreen, Color.IndianRed }),
            MessageBoxButtons.YesNoCancel => (new[] { Yes, No, Cancel }, new[] { Color.SeaGreen, Color.IndianRed, Color.DimGray }),
            MessageBoxButtons.YesNo => (new[] { Yes, No }, new[] { Color.SeaGreen, Color.IndianRed }),
            MessageBoxButtons.RetryCancel => (new[] { Retry, Cancel }, new[] { Color.SeaGreen, Color.DimGray }),
            _ => throw new ArgumentOutOfRangeException(nameof(btns))
        };

        private static MsgBoxLang? Lang(string name) => name == null ? null : (MsgBoxLang)Enum.Parse(typeof(MsgBoxLang), name);

        private static Button[] Slots(Form frm) => new[] { Priv.Field<Button>(frm, "btn1"), Priv.Field<Button>(frm, "btn2"), Priv.Field<Button>(frm, "btn3") };

        // Everything the box shows (sizes, texts, fonts, colors, icon, buttons and keys) as one comparable string
        private static string Snapshot(YANMessageBoxScreen frm)
        {
            var sb = new StringBuilder();
            sb.Append($"{frm.Size} {frm.BackColor.ToArgb():X} top={frm.TopMost} accept={(frm.AcceptButton as Control)?.Name} cancel={(frm.CancelButton as Control)?.Name} active={frm.ActiveControl?.Name}\n");
            foreach (var name in PARTS)
            {
                var c = Priv.Field<Control>(frm, name);
                sb.Append($"{name} {c.Bounds} {c.Visible} [{c.Text}] {c.Font.Name}/{c.Font.SizeInPoints}/{c.Font.Style} {c.BackColor.ToArgb():X} {c.ForeColor.ToArgb():X} {c.Padding} {c.MaximumSize}");
                if (c is Button b)
                {
                    sb.Append($" {b.DialogResult} {b.FlatAppearance.BorderSize} {b.FlatAppearance.BorderColor.ToArgb():X} {b.FlatAppearance.MouseOverBackColor.ToArgb():X}");
                }
                if (c is PictureBox { Image: Bitmap bmp })
                {
                    sb.Append($" {bmp.Size} {bmp.GetPixel(bmp.Width / 2, bmp.Height / 2).ToArgb():X}");
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        // Shows the box modally and runs act once it is on screen; act closes it (a click, a key). Fails instead of hanging
        private static DialogResult ShowModal(YANMessageBoxScreen frm, Action<YANMessageBoxScreen> act)
        {
            using var watcher = new Watcher(act);
            var res = frm.ShowDialog();
            watcher.Check();
            return res;
        }

        // Runs show (a YANMessageBox.Show call) and, once the box is on screen, snapshots it and clicks the button that press picks
        private static (string Snapshot, DialogResult Result) ShowAndPress(Func<DialogResult> show, Func<YANMessageBoxScreen, IButtonControl> press)
        {
            string snapshot = null;
            using var watcher = new Watcher(box =>
            {
                snapshot = Snapshot(box);
                ((Button)press(box)).PerformClick();
            });
            var res = show();
            watcher.Check();
            return (snapshot, res);
        }
        #endregion

        #region Nested types
        // Runs act on the next message box of this thread once it is on screen (act must close it). A box still open 10 s later is
        // hidden, which ends its ShowDialog, and Check fails the test instead of letting it hang
        private sealed class Watcher : IDisposable
        {
            private const int TIMEOUT_MS = 10000;
            private readonly System.Windows.Forms.Timer _timer = new() { Interval = 20 };
            private readonly Stopwatch _sw = Stopwatch.StartNew();
            private YANMessageBoxScreen _box;
            private Exception _error;
            private bool _is_TimedOut;

            public Watcher(Action<YANMessageBoxScreen> act)
            {
                _timer.Tick += (_, _) =>
                {
                    if (_box == null)
                    {
                        _box = Application.OpenForms.OfType<YANMessageBoxScreen>().FirstOrDefault(f => !f.IsDisposed && f.Visible && !f.InvokeRequired);
                        if (_box == null)
                        {
                            return;
                        }
                        _sw.Restart();
                        try
                        {
                            act(_box);
                        }
                        catch (Exception ex)
                        {
                            _error = ex;
                            _box.Hide();
                        }
                    }
                    else if (_box.IsDisposed || !_box.Visible)
                    {
                        _timer.Stop();
                    }
                    else if (_sw.ElapsedMilliseconds > TIMEOUT_MS)
                    {
                        _is_TimedOut = true;
                        _timer.Stop();
                        _box.Hide();
                    }
                };
                _timer.Start();
            }

            // Rethrows what act threw; fails when no box was shown or it did not close
            public void Check()
            {
                if (_error != null)
                {
                    ExceptionDispatchInfo.Capture(_error).Throw();
                }
                Assert.True(_box != null, "no message box was shown");
                Assert.False(_is_TimedOut, "the message box did not close");
            }

            public void Dispose() => _timer.Dispose();
        }
        #endregion
    }
}
