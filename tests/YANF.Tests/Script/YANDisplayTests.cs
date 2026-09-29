using System.Drawing;
using System.Windows.Forms;
using Xunit;
using YANF.Script;

namespace YANF.Tests.Script
{
    // HighLightLblLinkByCtrl: 1.0.2 created a new Font on every call (GDI leak on every Enter/Leave), threw for short names,
    // parentless controls and same-named non-Label controls, and dropped the label's other style bits
    public class YANDisplayTests
    {
        [Fact]
        public void HighLight_AllocatesFontOnlyWhenBoldChanges() => Sta.Run(ui =>
        {
            using var frm = new Form();
            using var consumerFont = new Font("Arial", 10f, FontStyle.Italic);
            var txt = new TextBox { Name = "txtName" };
            var lbl = new Label { Name = "lblName", Font = consumerFont };
            frm.Controls.Add(txt);
            frm.Controls.Add(lbl);
            // the demo pattern: isBold = false on an already regular label
            for (var i = 0; i < 50; i++)
            {
                txt.HighLightLblLinkByCtrl("txt", i % 2 == 0 ? Color.LightYellow : Color.WhiteSmoke, false);
                Assert.Same(consumerFont, lbl.Font);
            }
            Assert.Equal(Color.WhiteSmoke, lbl.ForeColor);
            // bold on: one new font, other style bits kept, consumer font untouched
            txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
            var bold = lbl.Font;
            Assert.NotSame(consumerFont, bold);
            Assert.True(bold.Bold && bold.Italic, "style bits lost: " + bold.Style);
            Assert.Equal(consumerFont.Name, bold.Name);
            Assert.Equal(consumerFont.Size, bold.Size);
            Assert.False(Gdi.IsDisposed(consumerFont), "consumer font disposed");
            Assert.Equal(Color.Red, lbl.ForeColor);
            // bold again: no allocation
            for (var i = 0; i < 10; i++)
            {
                txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
                Assert.Same(bold, lbl.Font);
            }
            // bold off: italic kept, the library-created bold font released, the consumer font still alive
            txt.HighLightLblLinkByCtrl("txt", Color.WhiteSmoke, false);
            var regular = lbl.Font;
            Assert.True(!regular.Bold && regular.Italic, "unbold style: " + regular.Style);
            Assert.NotSame(consumerFont, regular);
            Assert.True(Gdi.IsDisposed(bold), "the library-created bold font was not disposed when replaced");
            Assert.False(Gdi.IsDisposed(consumerFont), "consumer font disposed");
            Assert.False(Gdi.IsDisposed(regular), "current font disposed");
            ui.Draw(lbl);
        });

        [Fact]
        public void HighLight_NeverDisposesInheritedOrConsumerFonts() => Sta.Run(() =>
        {
            using var frm = new Form { Font = new Font("Arial", 9f, FontStyle.Underline) };
            var formFont = frm.Font;
            var pnl = new Panel();
            var dp = new DateTimePicker { Name = "dpBirth" };
            var lbl = new Label { Name = "lblBirth" };
            pnl.Controls.Add(lbl);
            frm.Controls.Add(pnl);
            frm.Controls.Add(dp);
            Assert.Same(formFont, lbl.Font);
            dp.HighLightLblLinkByCtrl("dp", Color.Yellow, false);
            Assert.Same(formFont, lbl.Font);
            dp.HighLightLblLinkByCtrl("dp", Color.Yellow, true);
            Assert.True(lbl.Font.Bold && lbl.Font.Underline, "bold + underline expected: " + lbl.Font.Style);
            Assert.False(Gdi.IsDisposed(formFont), "inherited form font disposed");
            Assert.Same(formFont, frm.Font);
            // a consumer font set between the calls is never disposed either
            using var consumerFont = new Font("Arial", 12f, FontStyle.Bold);
            lbl.Font = consumerFont;
            dp.HighLightLblLinkByCtrl("dp", Color.Yellow, false);
            Assert.False(lbl.Font.Bold);
            Assert.Equal(12f, lbl.Font.Size);
            Assert.False(Gdi.IsDisposed(consumerFont), "consumer font disposed");
            Assert.False(Gdi.IsDisposed(formFont), "form font disposed");
        });

        [Fact]
        public void HighLight_Guards() => Sta.Run(() =>
        {
            // not on a form
            using var lone = new TextBox { Name = "txtLone" };
            lone.HighLightLblLinkByCtrl("txt", Color.Red, true);

            using var frm = new Form();
            var shortName = new TextBox { Name = "tx" };
            var noName = new TextBox();
            var txt = new TextBox { Name = "txtCity" };
            var notLabel = new Button { Name = "lblCity", Text = "b" };
            frm.Controls.AddRange(new System.Windows.Forms.Control[] { shortName, noName, txt, notLabel });
            var btnFont = notLabel.Font;
            shortName.HighLightLblLinkByCtrl("txt", Color.Red, true);
            noName.HighLightLblLinkByCtrl("txt", Color.Red, true);
            txt.HighLightLblLinkByCtrl(null, Color.Red, true);
            ((System.Windows.Forms.Control)null).HighLightLblLinkByCtrl("txt", Color.Red, true);
            // only a non-Label matches: nothing happens (1.0.2 threw InvalidCastException)
            txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
            Assert.NotEqual(Color.Red, notLabel.ForeColor);
            Assert.Same(btnFont, notLabel.Font);
            // a Label further down with the same name is found past the non-Label
            var inner = new Panel();
            var lbl = new Label { Name = "lblCity" };
            inner.Controls.Add(lbl);
            frm.Controls.Add(inner);
            txt.HighLightLblLinkByCtrl("txt", Color.Red, true);
            Assert.Equal(Color.Red, lbl.ForeColor);
            Assert.True(lbl.Font.Bold);
        });
    }
}
