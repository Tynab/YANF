using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.TextRenderer;
using static YANF.Control.YANPaint;

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
        // the circle, its checked dot and the gap before the text, in 96-dpi pixels
        private const int CIRCLE_SIZE = 18;
        private const int DOT_SIZE = 12;
        private const int TEXT_GAP = 8;
        #endregion

        #region Constructors
        public YANRdo()
        {
            MinimumSize = new Size(0, 21);
            Padding = new Padding(10, 0, 0, 0);
            Font = new Font(Font.Name, 10f);
            // the parent is painted behind the control: repaint when it moves or when the parent changes behind it
            FollowParent(this);
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
            var isHighContrast = IsHighContrast;
            // draw surface: the parent's own pixels (a gradient, an image), under the radio button's own BackColor if it has one
            PaintSurface(e, isHighContrast);
            graphics.SmoothingMode = AntiAlias;
            var dpi = GetDpi(this);
            float rbBorderSize = LogicalToDevice(CIRCLE_SIZE, dpi);
            float rbCheckSize = LogicalToDevice(DOT_SIZE, dpi);
            var rectRbBorder = new RectangleF()
            {
                X = LogicalToDevice(0.5f, dpi),
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
            // drawing (the text color in high contrast mode)
            using var penBorder = new Pen(isHighContrast ? SystemColors.ControlText : _checkedColor, LogicalToDevice(1.6f, dpi));
            using var brushRbCheck = new SolidBrush(penBorder.Color);
            // draw radio button
            if (Checked)
            {
                graphics.DrawEllipse(penBorder, rectRbBorder);
                graphics.FillEllipse(brushRbCheck, rectRbCheck);
            }
            else
            {
                penBorder.Color = isHighContrast ? SystemColors.ControlText : _unCheckedColor;
                graphics.DrawEllipse(penBorder, rectRbBorder);
            }
            // draw text with TextRenderer, which also measures it, so the drawn text matches its measured size; '&' marks the mnemonic
            var flags = GetTextFlags();
            var textSize = MeasureText(Text, Font, Size.Empty, flags);
            var textLocation = new Point((int)rbBorderSize + LogicalToDevice(TEXT_GAP, dpi), (Height - textSize.Height) / 2);
            DrawText(graphics, Text, Font, textLocation, GetTextColor(isHighContrast), flags);
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
        // Paint what is behind the circle and the text: the parent, then the radio button's BackColor when it has a color of its own,
        // as a RadioButton does (a BackColor that is the parent's, the ambient default, shows the parent's real background, and a
        // transparent one too). High contrast mode paints the system control color
        private void PaintSurface(PaintEventArgs e, bool isHighContrast)
        {
            var backColor = isHighContrast ? SystemColors.Control : BackColor;
            // without a parent, PaintParent paints the BackColor
            var isOwnColor = isHighContrast || (Parent is { } parent && backColor.ToArgb() != parent.BackColor.ToArgb());
            // an opaque color of its own hides the parent: its painting (and its Paint handlers) would be wasted
            if (!isOwnColor || backColor.A < 255)
            {
                PaintParent(this, e);
            }
            if (isOwnColor && backColor.A > 0)
            {
                using var brushBack = new SolidBrush(backColor);
                e.Graphics.FillRectangle(brushBack, ClientRectangle);
            }
        }

        // Get the text flags: '&' marks the mnemonic (underlined, or hidden until ALT is pressed when Windows hides the keyboard
        // cues) as in a RadioButton; with UseMnemonic = false it is drawn as it is
        private TextFormatFlags GetTextFlags() => !UseMnemonic ? TextFormatFlags.NoPrefix : ShowKeyboardCues ? TextFormatFlags.Default : TextFormatFlags.HidePrefix;

        // Get the painted text color: HighlightText under the mouse pointer (the ForeColor property never changes); system colors
        // in high contrast mode
        private Color GetTextColor(bool isHighContrast) => isHighContrast ? _is_Hover ? SystemColors.HotTrack : SystemColors.ControlText : _is_Hover ? _highlightText : ForeColor;

        // Draw the keyboard focus cue (with the system colors in high contrast mode)
        private void DrawFocusCue(Graphics graphics, Rectangle rectCue)
        {
            if (rectCue.Width <= 0 || rectCue.Height <= 0)
            {
                return;
            }
            graphics.SmoothingMode = None;
            if (IsHighContrast)
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