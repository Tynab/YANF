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
    [ToolboxBitmap(typeof(RadioButton))]
    public class YANRdo : RadioButton
    {
        #region Fields
        private Color _checkedColor = MediumSlateBlue;
        private Color _unCheckedColor = Gray;
        private Color _highlightText = DarkGoldenrod;
        private bool _is_Hover = false;
        // '&' is drawn as it is, as 1.0 drew it with Graphics.DrawString (a mnemonic underline would change the text of existing forms)
        private const TextFormatFlags TEXT_FLAGS = TextFormatFlags.NoPrefix;
        #endregion

        #region Constructors
        public YANRdo()
        {
            MinimumSize = new Size(0, 21);
            Padding = new Padding(10, 0, 0, 0);
            Font = new Font(Font.Name, 10f);
        }
        #endregion

        #region Properties
        [Category("YAN Appearance"), Description("The color of the circle when the control is checked.")]
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
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

        [Category("YAN Appearance"), Description("The color of the circle when the control is unchecked.")]
        [DefaultValue(typeof(Color), "Gray")]
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
        [DefaultValue(typeof(Color), "DarkGoldenrod")]
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
            // draw text with TextRenderer, which also measures it, so the drawn text matches its measured size
            var textSize = MeasureText(Text, Font, Size.Empty, TEXT_FLAGS);
            var textLocation = new Point((int)rbBorderSize + 8, (Height - textSize.Height) / 2);
            DrawText(graphics, Text, Font, textLocation, _is_Hover ? _highlightText : ForeColor, TEXT_FLAGS);
            // draw the keyboard focus cue around the text, or the circle when there is no text (Windows hides it until the
            // keyboard is used); ButtonBase repaints on focus changes
            if (Focused && ShowFocusCues)
            {
                var rectCue = string.IsNullOrEmpty(Text) ? Rectangle.Round(RectangleF.Inflate(rectRbBorder, 1, 1)) : new Rectangle(textLocation, textSize);
                rectCue.Intersect(ClientRectangle);
                DrawFocusCue(graphics, rectCue);
            }
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

        #region Methods
        // Draw the keyboard focus cue (with the system colors in high contrast mode)
        private void DrawFocusCue(Graphics graphics, Rectangle rectCue)
        {
            if (rectCue.Width <= 0 || rectCue.Height <= 0)
            {
                return;
            }
            graphics.SmoothingMode = None;
            if (SystemInformation.HighContrast)
            {
                ControlPaint.DrawFocusRectangle(graphics, rectCue);
            }
            else
            {
                ControlPaint.DrawFocusRectangle(graphics, rectCue, ForeColor, BackColor);
            }
        }
        #endregion
    }
}