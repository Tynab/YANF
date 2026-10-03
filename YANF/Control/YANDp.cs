using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Math;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.TextRenderer;
using static YANF.Control.YANPaint;
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
        // the rectangular border band (BorderSize, in 96-dpi pixels), built for the current size and DPI
        private readonly YANBorder _border;
        // the calendar icon for the painted skin (black on a light skin), scaled to _calIcDpi; loaded at the first paint
        private Image _calIc;
        private bool _is_CalIcBlack;
        private int _calIcDpi;
        private RectangleF _icBtnArea;
        private bool _is_DroppedDown = false;
        private bool _is_FollowingParent = false;
        // the icon button: the calendar icon when the text leaves room for it, otherwise its arrow part only; the room the text must
        // leave for it, and the distance of the icon from the right edge (96-dpi pixels)
        private const int CAL_IC_WIDTH = 34;
        private const int ARROW_IC_WIDTH = 17;
        private const int TEXT_ROOM = 20;
        private const int CAL_IC_RIGHT = 9;
        // the distance of the focus cue inside the border (96-dpi pixels)
        private const int FOCUS_CUE_GAP = 2;
        // the text is indented by three spaces of its font, as in 1.x
        private const string TEXT_INDENT = "   ";
        #endregion

        #region Constructors
        public YANDp()
        {
            // paint everything in WM_PAINT through a back buffer
            SetStyle(UserPaint | AllPaintingInWmPaint | OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            _border = new YANBorder(this, 0, 0);
            // property
            MinimumSize = new Size(0, 35);
            Font = new Font(Font.Name, 10f);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the color of the surface. Where it is not opaque, the parent's own pixels show through it (1.x showed the
        /// window color).
        /// </summary>
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
                    // the parent shows through a skin that is not opaque: repaint when it changes behind the control
                    if (value.A < 255 && !_is_FollowingParent)
                    {
                        _is_FollowingParent = true;
                        FollowParent(this);
                    }
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

        /// <summary>
        /// Gets or sets the width of the border in 96-dpi pixels: it is scaled with the DPI of the control (2.0), and clamped to half
        /// the smaller side when painted.
        /// </summary>
        [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
        [DefaultValue(0)]
        public int BorderSize
        {
            get => _border.Size;
            set
            {
                value = Max(0, value);
                if (_border.Size != value)
                {
                    _border.Size = value;
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // the skin covers the whole control: where it is not opaque, the parent's own pixels show through it
            if (GetSkinColor().A < 255)
            {
                PaintParent(this, e);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }
            var graphics = e.Graphics;
            var dpi = GetDpi(this);
            var skinColor = GetSkinColor();
            var rectSurface = ClientRectangle;
            // draw surface
            if (skinColor.A > 0)
            {
                using var skinBrush = new SolidBrush(skinColor);
                graphics.FillRectangle(skinBrush, rectSurface);
            }
            // draw text with TextRenderer, which also measures it (the icon button area), laid out as in 1.x: indented, centred
            // vertically, wrapped at words when it is too long
            DrawText(graphics, TEXT_INDENT + Text, Font, rectSurface, IsHighContrast ? SystemColors.WindowText : _textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            // draw open calendar icon highlight
            if (_is_DroppedDown)
            {
                var wCalIc = LogicalToDevice(CAL_IC_WIDTH, dpi);
                using var openIcBrush = new SolidBrush(IsHighContrast ? SystemColors.Highlight : FromArgb(50, 64, 64, 64));
                graphics.FillRectangle(openIcBrush, new Rectangle(Width - wCalIc, 0, wCalIc, Height));
            }
            // draw border
            _border.DrawBorder(graphics, Contrast(_borderColor, SystemColors.WindowFrame));
            // draw icon, at its size in device pixels (the image is scaled once per DPI)
            var calIc = GetCalIc(dpi, skinColor);
            graphics.DrawImage(calIc, new Rectangle(Width - calIc.Width - LogicalToDevice(CAL_IC_RIGHT, dpi), (Height - calIc.Height) / 2, calIc.Width, calIc.Height));
            // draw the keyboard focus cue inside the border (Windows hides it until the keyboard is used)
            if (Focused && ShowFocusCues)
            {
                var inset = _border.DeviceSize + LogicalToDevice(FOCUS_CUE_GAP, dpi);
                DrawFocusCue(graphics, Rectangle.Inflate(rectSurface, -inset, -inset), skinColor);
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

        protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
        {
            base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
            // the border, the icon and the icon button area are rebuilt for the new DPI
            _border.Reset();
            ResetCalIc();
            UpdateIcBtnArea();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _border.Dispose();
                ResetCalIc();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Methods
        // Get width of icon of button (96-dpi widths scaled to the DPI of the control)
        private int GetWIcBtn()
        {
            var dpi = GetDpi(this);
            var wCalIc = LogicalToDevice(CAL_IC_WIDTH, dpi);
            return MeasureText(Text, Font).Width <= Width - wCalIc - LogicalToDevice(TEXT_ROOM, dpi) ? wCalIc : LogicalToDevice(ARROW_IC_WIDTH, dpi);
        }

        // Update the area of icon of button when the size, font, format, text or DPI changes
        private void UpdateIcBtnArea()
        {
            var wIc = GetWIcBtn();
            _icBtnArea = new RectangleF(Width - wIc, 0, wIc, Height);
        }

        // Get the painted skin color: the system window color in high contrast mode (the configured value is never changed)
        private Color GetSkinColor() => Contrast(_skinColor, SystemColors.Window);

        // Get the calendar icon for the painted skin (black on a light skin) at the DPI: loaded, and scaled from its 96-dpi size, once
        // per change of the two (each resource read creates a new image, which the control owns)
        private Image GetCalIc(int dpi, Color skinColor)
        {
            var isBlack = skinColor.GetBrightness() >= 0.8f;
            if (_calIc != null && _is_CalIcBlack == isBlack && _calIcDpi == dpi)
            {
                return _calIc;
            }
            ResetCalIc();
            Image calIc = isBlack ? pCalendarBlack : pCalendarWhite;
            var size = new Size(LogicalToDevice(calIc.Width, dpi), LogicalToDevice(calIc.Height, dpi));
            if (size != calIc.Size && size.Width > 0 && size.Height > 0)
            {
                var scaled = new Bitmap(size.Width, size.Height);
                using (var g = Graphics.FromImage(scaled))
                using (var attributes = new ImageAttributes())
                {
                    // the edges are sampled from the image itself, not blended with the transparent pixels around it
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(calIc, new Rectangle(Point.Empty, size), 0, 0, calIc.Width, calIc.Height, GraphicsUnit.Pixel, attributes);
                }
                calIc.Dispose();
                calIc = scaled;
            }
            _calIc = calIc;
            _is_CalIcBlack = isBlack;
            _calIcDpi = dpi;
            return calIc;
        }

        // Release the calendar icon; it is loaded again at the next paint
        private void ResetCalIc()
        {
            _calIc?.Dispose();
            _calIc = null;
        }

        // Draw the keyboard focus cue (with the system colors in high contrast mode)
        private void DrawFocusCue(Graphics graphics, Rectangle rectCue, Color skinColor)
        {
            if (rectCue.Width <= 0 || rectCue.Height <= 0)
            {
                return;
            }
            if (IsHighContrast)
            {
                ControlPaint.DrawFocusRectangle(graphics, rectCue);
            }
            else
            {
                ControlPaint.DrawFocusRectangle(graphics, rectCue, _textColor, skinColor);
            }
        }
        #endregion
    }
}
