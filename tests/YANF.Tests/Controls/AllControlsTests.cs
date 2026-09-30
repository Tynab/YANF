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

        // The focus cue is drawn only while the control has the focus and the focus cues are shown
        [Theory]
        [InlineData(typeof(YANBtn), typeof(FocusedBtn))]
        [InlineData(typeof(YANTg), typeof(FocusedTg))]
        [InlineData(typeof(YANRdo), typeof(FocusedRdo))]
        [InlineData(typeof(YANDp), typeof(FocusedDp))]
        public void FocusCue_DrawnOnlyWhenFocused(Type plain, Type focused) => Sta.Run(ui =>
        {
            using var bmpPlain = ui.Render(Setup((Control)Activator.CreateInstance(plain)));
            using var bmpFocused = ui.Render(Setup((Control)Activator.CreateInstance(focused)));
            Assert.True(Differences(bmpPlain, bmpFocused) > 0, "no focus cue drawn");
            // the same control without the focus: no cue
            using var bmpPlain2 = ui.Render(Setup((Control)Activator.CreateInstance(plain)));
            Assert.Equal(0, Differences(bmpPlain, bmpPlain2));
        });

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
