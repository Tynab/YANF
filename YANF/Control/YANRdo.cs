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
        private Color _highlightText = DarkGoldenrod;
        private bool _is_Hover = false;
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
                if (_checkedColor != value)
                {
                    _checkedColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The color of the control when the control set to unchecked.")]
        public Color UnCheckedColor
        {
            get => _unCheckedColor;
            set
            {
                if (_unCheckedColor != value)
                {
                    _unCheckedColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The color of the text when the mouse pointer is over the control.")]
        public Color HighlightText
        {
            get => _highlightText;
            set
            {
                if (_highlightText != value)
                {
                    _highlightText = value;
                    if (_is_Hover)
                    {
                        Invalidate();
                    }
                }
            }
        }
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
            using var brushText = new SolidBrush(_is_Hover ? _highlightText : ForeColor);
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

        protected override Cursor DefaultCursor => Hand;

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            // hover only changes the painted text color, never the ForeColor property
            _is_Hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _is_Hover = false;
            Invalidate();
        }
        #endregion
    }
}