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
    }
}
