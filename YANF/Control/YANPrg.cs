using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.ComponentModel.EditorBrowsableState;
using static System.Drawing.Color;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.TextRenderer;
using static YANF.Control.YANPaint;
using static YANF.Script.YANConstant;
using static YANF.Script.YANConstant.PrgTextPosition;

namespace YANF.Control
{
    [ToolboxBitmap(typeof(ProgressBar))]
    public class YANPrg : ProgressBar
    {
        #region Fields
        private Color _channelColor = LightSteelBlue;
        private Color _sliderColor = RoyalBlue;
        private Color _valueBackColor = RoyalBlue;
        private PrgTextPosition _textAlign = PrgTextPosition.Right;
        private string _symbolBefore = null;
        private string _symbolAfter = null;
        private int _channelHeight = 6;
        private int _sliderHeight = 6;
        private bool _is_ShowMaximum = false;
        // the value is measured and drawn with the same GDI flags ('&' is data, not a mnemonic)
        private const TextFormatFlags TEXT_FLAGS = TextFormatFlags.NoPrefix;
        #endregion

        #region Constructors
        public YANPrg()
        {
            // paint everything on every paint, through a back buffer, so the bar never shows a blank or stale surface
            SetStyle(UserPaint | AllPaintingInWmPaint | OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // property
            ForeColor = White;
            // the parent is painted behind the control: repaint when it moves or when the parent changes behind it
            FollowParent(this);
        }
        #endregion

        #region Properties
        [Category("YAN Appearance"), Description("The color of the channel.")]
        [DefaultValue(typeof(Color), "LightSteelBlue")]
        public Color ChannelColor
        {
            get => _channelColor;
            set
            {
                if (_channelColor != value)
                {
                    _channelColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The color of the slider.")]
        [DefaultValue(typeof(Color), "RoyalBlue")]
        public Color SliderColor
        {
            get => _sliderColor;
            set
            {
                if (_sliderColor != value)
                {
                    _sliderColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The background color of the value.")]
        [DefaultValue(typeof(Color), "RoyalBlue")]
        public Color ValueBackColor
        {
            get => _valueBackColor;
            set
            {
                if (_valueBackColor != value)
                {
                    _valueBackColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("Indicates where the value is displayed on the control (None hides it).")]
        [DefaultValue(PrgTextPosition.Right)]
        public PrgTextPosition TextAlign
        {
            get => _textAlign;
            set
            {
                if (_textAlign != value)
                {
                    _textAlign = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The symbol that is displayed before the value on the control.")]
        [DefaultValue(null)]
        public string SymbolBefore
        {
            get => _symbolBefore;
            set
            {
                if (_symbolBefore != value)
                {
                    _symbolBefore = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The symbol that is displayed after the value on the control.")]
        [DefaultValue(null)]
        public string SymbolAfter
        {
            get => _symbolAfter;
            set
            {
                if (_symbolAfter != value)
                {
                    _symbolAfter = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The height of the channel in pixels (at 96 dpi; it scales with the DPI).")]
        [DefaultValue(6)]
        public int ChannelHeight
        {
            get => _channelHeight;
            set
            {
                if (value >= 0 && _channelHeight != value)
                {
                    _channelHeight = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The height of the slider in pixels (at 96 dpi; it scales with the DPI).")]
        [DefaultValue(6)]
        public int SliderHeight
        {
            get => _sliderHeight;
            set
            {
                if (value >= 0 && _sliderHeight != value)
                {
                    _sliderHeight = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("When this property is true, the maximum value is displayed after the value.")]
        [DefaultValue(false)]
        public bool ShowMaximum
        {
            get => _is_ShowMaximum;
            set
            {
                if (_is_ShowMaximum != value)
                {
                    _is_ShowMaximum = value;
                    Invalidate();
                }
            }
        }
        #endregion

        #region Overridden
        [Category("Appearance"), Description("The font used to display the value.")]
        [Browsable(true)]
        [EditorBrowsable(Always)]
        public override Font Font { get => base.Font; set => base.Font = value; }

        [Category("Appearance"), Description("The color of the value.")]
        [DefaultValue(typeof(Color), "White")]
        public override Color ForeColor { get => base.ForeColor; set => base.ForeColor = value; }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            // painting surface: the parent's own pixels (a gradient, an image), not a block of Parent.BackColor
            PaintParent(this, e);
            // channel, at the bottom of the client area (a progress bar without visual styles has a 1-pixel frame around it)
            var (channelHeight, sliderHeight) = GetBarHeights();
            var client = ClientSize;
            var rectChannel = new Rectangle(0, 0, client.Width, channelHeight);
            rectChannel.Y = channelHeight >= sliderHeight ? client.Height - channelHeight : client.Height - (channelHeight + sliderHeight) / 2;
            using var brushChannel = new SolidBrush(Contrast(_channelColor, SystemColors.ControlText));
            graphics.FillRectangle(brushChannel, rectChannel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            // an empty range (Minimum == Maximum) has no progress to show
            var client = ClientSize;
            var wSlider = Maximum > Minimum ? (int)(client.Width * ((double)Value - Minimum) / ((double)Maximum - Minimum)) : 0;
            var (channelHeight, sliderHeight) = GetBarHeights();
            var rectSlider = new Rectangle(0, 0, wSlider, sliderHeight);
            using var brushSlider = new SolidBrush(Contrast(_sliderColor, SystemColors.Highlight));
            rectSlider.Y = sliderHeight >= channelHeight ? client.Height - sliderHeight : client.Height - (sliderHeight + channelHeight) / 2;
            // painting slider
            if (wSlider > 1)
            {
                graphics.FillRectangle(brushSlider, rectSlider);
            }
            // painting text
            if (_textAlign != None)
            {
                DrawValueText(graphics, wSlider, rectSlider);
            }
        }
        #endregion

        #region Methods
        // Get the heights of the channel and of the slider in device pixels (the properties are 96-dpi pixels)
        private (int Channel, int Slider) GetBarHeights()
        {
            var dpi = GetDpi(this);
            return (LogicalToDevice(_channelHeight, dpi), LogicalToDevice(_sliderHeight, dpi));
        }

        // Draw the value on its background, with TextRenderer, which also measures it (the drawn text fits its measured box)
        private void DrawValueText(Graphics graphics, int wSlider, Rectangle rectSlider)
        {
            var text = $"{_symbolBefore}{Value}{_symbolAfter}";
            if (_is_ShowMaximum)
            {
                text += $"/{_symbolBefore}{Maximum}{_symbolAfter}";
            }
            var textSize = MeasureText(text, Font, Size.Empty, TEXT_FLAGS);
            var rectText = new Rectangle(0, 0, textSize.Width, textSize.Height + LogicalToDevice(this, 2));
            switch (_textAlign)
            {
                case PrgTextPosition.Left:
                {
                    rectText.X = 0;
                    break;
                }
                case PrgTextPosition.Right:
                {
                    rectText.X = ClientSize.Width - textSize.Width;
                    break;
                }
                case PrgTextPosition.Center:
                {
                    rectText.X = (ClientSize.Width - textSize.Width) / 2;
                    break;
                }
                case Sliding:
                {
                    rectText.X = wSlider - textSize.Width;
                    // clean previous surface: the band of the text over the progress shows the parent again
                    var rect = rectSlider;
                    rect.Y = rectText.Y;
                    rect.Height = rectText.Height;
                    PaintParent(this, graphics, rect);
                    break;
                }
            }
            // painting (system colors in high contrast mode)
            using var brushTextBack = new SolidBrush(Contrast(_valueBackColor, SystemColors.Highlight));
            graphics.FillRectangle(brushTextBack, rectText);
            DrawText(graphics, text, Font, rectText, Contrast(ForeColor, SystemColors.HighlightText), TEXT_FLAGS | TextFormatFlags.HorizontalCenter);
        }
        #endregion
    }
}
