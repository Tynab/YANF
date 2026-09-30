using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.ComponentModel.EditorBrowsableState;
using static System.Drawing.Color;
using static System.Drawing.StringAlignment;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.TextRenderer;
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
        #endregion

        #region Constructors
        public YANPrg()
        {
            // paint everything on every paint, through a back buffer, so the bar never shows a blank or stale surface
            SetStyle(UserPaint | AllPaintingInWmPaint | OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // property
            ForeColor = White;
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

        [Category("YAN Appearance"), Description("The height of the channel in pixels.")]
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

        [Category("YAN Appearance"), Description("The height of the slider in pixels.")]
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
            var rectChannel = new Rectangle(0, 0, Width, ChannelHeight);
            using var brushChannel = new SolidBrush(_channelColor);
            rectChannel.Y = _channelHeight >= _sliderHeight ? Height - _channelHeight : Height - (_channelHeight + _sliderHeight) / 2;
            // painting surface
            graphics.Clear(Parent?.BackColor ?? BackColor);
            //channel
            graphics.FillRectangle(brushChannel, rectChannel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            // an empty range (Minimum == Maximum) has no progress to show
            var wSlider = Maximum > Minimum ? (int)(Width * ((double)Value - Minimum) / ((double)Maximum - Minimum)) : 0;
            var rectSlider = new Rectangle(0, 0, wSlider, SliderHeight);
            using var brushSlider = new SolidBrush(_sliderColor);
            rectSlider.Y = _sliderHeight >= _channelHeight ? Height - _sliderHeight : Height - (_sliderHeight + _channelHeight) / 2;
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

        private void DrawValueText(Graphics graphics, int wSlider, Rectangle rectSlider)
        {
            var text = $"{_symbolBefore}{Value}{_symbolAfter}";
            if (_is_ShowMaximum)
            {
                text += $"/{_symbolBefore}{Maximum}{_symbolAfter}";
            }
            var textSize = MeasureText(text, Font);
            var rectText = new Rectangle(0, 0, textSize.Width, textSize.Height + 2);
            using var brushText = new SolidBrush(ForeColor);
            using var brushTextBack = new SolidBrush(_valueBackColor);
            using var textFormat = new StringFormat();
            switch (_textAlign)
            {
                case PrgTextPosition.Left:
                {
                    rectText.X = 0;
                    textFormat.Alignment = Near;
                    break;
                }
                case PrgTextPosition.Right:
                {
                    rectText.X = Width - textSize.Width;
                    textFormat.Alignment = Far;
                    break;
                }
                case PrgTextPosition.Center:
                {
                    rectText.X = (Width - textSize.Width) / 2;
                    textFormat.Alignment = StringAlignment.Center;
                    break;
                }
                case Sliding:
                {
                    rectText.X = wSlider - textSize.Width;
                    textFormat.Alignment = StringAlignment.Center;
                    // clean previous surface
                    using var brushClear = new SolidBrush(Parent?.BackColor ?? BackColor);
                    var rect = rectSlider;
                    rect.Y = rectText.Y;
                    rect.Height = rectText.Height;
                    graphics.FillRectangle(brushClear, rect);
                    break;
                }
            }
            // painting
            graphics.FillRectangle(brushTextBack, rectText);
            graphics.DrawString(text, Font, brushText, rectText, textFormat);
        }
        #endregion
    }
}