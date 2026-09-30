using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Math;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.TextRenderer;
using static YANF.Properties.Resources;

namespace YANF.Control
{
    [ToolboxBitmap(typeof(DateTimePicker))]
    public class YANDp : DateTimePicker
    {
        #region Fields
        private Color _skinColor = MediumSlateBlue;
        private Color _textColor = White;
        private Color _borderColor = PaleVioletRed;
        private int _borderSize = 0;
        private Image _calIc = pCalendarWhite;
        private RectangleF _icBtnArea;
        private const byte _wCalIc = 34;
        private const byte _wArrowIc = 17;
        private bool _is_DroppedDown = false;
        #endregion

        #region Constructors
        public YANDp()
        {
            // paint everything in WM_PAINT through a back buffer
            SetStyle(UserPaint | AllPaintingInWmPaint | OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // property
            MinimumSize = new Size(0, 35);
            Font = new Font(Font.Name, 10f);
        }
        #endregion

        #region Properties
        [Category("YAN Appearance"), Description("The background color of the component.")]
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
        public Color SkinColor
        {
            get => _skinColor;
            set
            {
                if (_skinColor != value)
                {
                    _skinColor = value;
                    _calIc = _skinColor.GetBrightness() >= 0.8f ? pCalendarBlack : pCalendarWhite;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The foreground color of this component, which is used to display text.")]
        [DefaultValue(typeof(Color), "White")]
        public Color TextColor
        {
            get => _textColor;
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("This property specifies the color of the border around the control.")]
        [DefaultValue(typeof(Color), "PaleVioletRed")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
        [DefaultValue(0)]
        public int BorderSize
        {
            get => _borderSize;
            set
            {
                value = Max(0, value);
                if (_borderSize != value)
                {
                    _borderSize = value;
                    Invalidate();
                }
            }
        }
        #endregion

        #region Overridden
        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            _is_DroppedDown = true;
            Invalidate();
        }

        protected override void OnCloseUp(EventArgs e)
        {
            base.OnCloseUp(e);
            _is_DroppedDown = false;
            Invalidate();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            // typed characters would edit the day, month or year field that the native control has selected, which UserPaint
            // never shows: they are ignored (the arrow keys and the drop-down calendar, F4 or ALT+DOWN, still work)
            e.Handled = true;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            // show the focus cue
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            // hide the focus cue
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            var graphics = e.Graphics;
            var borderSize = GetBorderSize();
            using var penBorder = new Pen(GetBorderColor(), borderSize);
            using var skinBrush = new SolidBrush(_skinColor);
            using var openIcBrush = new SolidBrush(FromArgb(50, 64, 64, 64));
            using var textBrush = new SolidBrush(_textColor);
            using var textFormat = new StringFormat();
            penBorder.Alignment = Inset;
            textFormat.LineAlignment = StringAlignment.Center;
            var clientArea = new RectangleF(0, 0, Width - 0.5f, Height - 0.5f);
            // draw surface
            graphics.FillRectangle(skinBrush, clientArea);
            // draw text
            graphics.DrawString("   " + Text, Font, textBrush, clientArea, textFormat);
            // draw open calendar icon highlight
            if (_is_DroppedDown)
            {
                graphics.FillRectangle(openIcBrush, new RectangleF(clientArea.Width - _wCalIc, 0, _wCalIc, clientArea.Height));
            }
            // draw border
            if (borderSize >= 1)
            {
                graphics.DrawRectangle(penBorder, clientArea.X, clientArea.Y, clientArea.Width, clientArea.Height);
            }
            // draw icon
            graphics.DrawImage(_calIc, Width - _calIc.Width - 9, (Height - _calIc.Height) / 2);
            // draw the keyboard focus cue inside the border (Windows hides it until the keyboard is used)
            if (Focused && ShowFocusCues)
            {
                DrawFocusCue(graphics, Rectangle.Inflate(ClientRectangle, -(borderSize + 2), -(borderSize + 2)));
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateIcBtnArea();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateIcBtnArea();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            UpdateIcBtnArea();
        }

        protected override void OnFormatChanged(EventArgs e)
        {
            base.OnFormatChanged(e);
            UpdateIcBtnArea();
            // the displayed text depends on the format
            Invalidate();
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            base.OnValueChanged(eventargs);
            UpdateIcBtnArea();
            // the text is painted as a whole, so repaint all of it
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // change the cursor only when the pointer crosses the icon border, not on every mouse move
            var cursor = _icBtnArea.Contains(e.Location) ? Hand : DefaultCursor;
            if (Cursor != cursor)
            {
                Cursor = cursor;
            }
        }
        #endregion

        #region Methods
        // Get width of icon of button
        private int GetWIcBtn() => MeasureText(Text, Font).Width <= Width - _wCalIc - 20 ? _wCalIc : _wArrowIc;

        // Update the area of icon of button when the size, font, format or text changes
        private void UpdateIcBtnArea()
        {
            var wIc = GetWIcBtn();
            _icBtnArea = new RectangleF(Width - wIc, 0, wIc, Height);
        }

        // Get the border size that fits the current size (the configured value is never changed)
        private int GetBorderSize() => Max(0, Min(_borderSize, Min(Width, Height) / 2));

        // Get the painted border color: the system frame color in high contrast mode (the configured value is never changed)
        private Color GetBorderColor() => SystemInformation.HighContrast ? SystemColors.WindowFrame : _borderColor;

        // Draw the keyboard focus cue (with the system colors in high contrast mode)
        private void DrawFocusCue(Graphics graphics, Rectangle rectCue)
        {
            if (rectCue.Width <= 0 || rectCue.Height <= 0)
            {
                return;
            }
            if (SystemInformation.HighContrast)
            {
                ControlPaint.DrawFocusRectangle(graphics, rectCue);
            }
            else
            {
                ControlPaint.DrawFocusRectangle(graphics, rectCue, _textColor, _skinColor);
            }
        }
        #endregion
    }
}