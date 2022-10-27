using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.TextRenderer;

namespace YANF.Control
{
    public class YANRdo : RadioButton
    {
        #region Fields
        private Color _checkedColor = MediumSlateBlue;
        private Color _unCheckedColor = Gray;
        private Color _foreColorTemp;
        #endregion

        #region Constructors
        public YANRdo()
        {
            TabStop = false;
            MinimumSize = new Size(0, 21);
            Padding = new Padding(10, 0, 0, 0);
            Font = new Font(Font.Name, 10f);
        }
        #endregion

        #region Properties
        [Category("YAN Appearance"), Description("The color of the control when the control set to checked.")]
        public Color CheckedColor
        {
            get => _checkedColor;
            set
            {
                _checkedColor = value;
                Invalidate();
            }
        }

        [Category("YAN Appearance"), Description("The color of the control when the control set to unchecked.")]
        public Color UnCheckedColor
        {
            get => _unCheckedColor;
            set
            {
                _unCheckedColor = value;
                Invalidate();
            }
        }

        [Category("YAN Appearance"), Description("The color of the text when the control have the focus.")]
        public Color HighlightText { get; set; } = DarkGoldenrod;
        #endregion

        #region Overridden
        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = AntiAlias;
            var rbBorderSize = 18f;
            var rbCheckSize = 12f;
            var rectRbBorder = new RectangleF()
            {
                X = 0.5f,
                Y = (Height - rbBorderSize) / 2, //center
                Width = rbBorderSize,
                Height = rbBorderSize
            };
            var rectRbCheck = new RectangleF()
            {
                X = rectRbBorder.X + (rectRbBorder.Width - rbCheckSize) / 2, //center
                Y = (Height - rbCheckSize) / 2, //center
                Width = rbCheckSize,
                Height = rbCheckSize
            };
            // drawing
            using var penBorder = new Pen(_checkedColor, 1.6f);
            using var brushRbCheck = new SolidBrush(_checkedColor);
            using var brushText = new SolidBrush(ForeColor);
            // draw surface
            graphics.Clear(BackColor);
            // draw radio button
            if (Checked)
            {
                graphics.DrawEllipse(penBorder, rectRbBorder);
                graphics.FillEllipse(brushRbCheck, rectRbCheck);
            }
            else
            {
                penBorder.Color = _unCheckedColor;
                graphics.DrawEllipse(penBorder, rectRbBorder);
            }
            // draw text
            graphics.DrawString(Text, Font, brushText, rbBorderSize + 8, (Height - MeasureText(Text, Font).Height) / 2);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _foreColorTemp = ForeColor;
            ForeColor = HighlightText;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            ForeColor = _foreColorTemp;
        }
        #endregion
    }
}