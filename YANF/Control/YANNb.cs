using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Math;
using static System.Windows.Forms.TextRenderer;
using static YANF.Script.YANShape;

namespace YANF.Control
{
    [DefaultEvent("ValueChanged")]
    public class YANNb : UserControl
    {
        #region Fields
        private Color _borderColor = MediumSlateBlue;
        private int _borderSize = 1;
        private int _borderRadius = 0;
        private bool _is_UnderlinedStyle = false;
        private bool _is_Focus = false;
        private readonly NumericUpDown _nudNum;
        #endregion

        #region Constructors
        public YANNb()
        {
            _nudNum = new NumericUpDown();
            SuspendLayout();
            // numeric num
            _nudNum.BackColor = White;
            _nudNum.BorderStyle = BorderStyle.None;
            _nudNum.TextAlign = HorizontalAlignment.Center;
            _nudNum.Dock = DockStyle.Fill;
            _nudNum.Location = new Point(10, 7);
            _nudNum.Size = new Size(180, 18);
            _nudNum.Font = new Font(Font.Name, 11f);
            _nudNum.Enter += Nud_Enter;
            _nudNum.Leave += Nud_Leave;
            _nudNum.KeyDown += Nud_KeyDown;
            _nudNum.KeyPress += Nud_KeyPress;
            _nudNum.ValueChanged += Nud_ValueChanged;
            _nudNum.SizeChanged += Nud_SizeChanged;
            // user control
            Controls.Add(_nudNum);
            DoubleBuffered = true;
            ResizeRedraw = true;
            ForeColor = DimGray;
            BackColor = White;
            ThousandsSeparator = true;
            AutoScaleMode = AutoScaleMode.None;
            String = Value.ToString();
            Size = new Size(200, 30);
            Padding = new Padding(10, 7, 10, 7);
            Font = new Font(Font.Name, 11f);
            // base
            ResumeLayout();
        }
        #endregion

        #region Properties
        public string String;

        [Category("YAN Appearance"), Description("Indicates how the text should be aligned for edit controls.")]
        public HorizontalAlignment TextAlign
        {
            get => _nudNum.TextAlign;
            set
            {
                if (_nudNum.TextAlign != value)
                {
                    _nudNum.TextAlign = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("This property specifies the color of the border around the control.")]
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

        [Category("YAN Appearance"), Description("This property specifies the color of the border around the control when the control have the focus.")]
        public Color BorderFocusColor { get; set; } = LightYellow;

        [Category("YAN Appearance"), Description("Indicates the minimum value for the numeric up-down control.")]
        public decimal Minimum
        {
            get => _nudNum.Minimum;
            set
            {
                // NumericUpDown raises Maximum and constrains Value itself, so Maximum and Minimum can be set in any order
                if (_nudNum.Minimum != value)
                {
                    _nudNum.Minimum = value;
                }
            }
        }

        [Category("YAN Appearance"), Description("Indicates the maximum value for the numeric up-down control.")]
        public decimal Maximum
        {
            get => _nudNum.Maximum;
            set
            {
                // NumericUpDown lowers Minimum and constrains Value itself, so Maximum and Minimum can be set in any order
                if (_nudNum.Maximum != value)
                {
                    _nudNum.Maximum = value;
                }
            }
        }

        [Category("YAN Appearance"), Description("The current value of the numeric up-down control.")]
        public decimal Value
        {
            get => _nudNum.Value;
            set
            {
                _nudNum.Value = Max(Minimum, Min(Maximum, value));
                String = _nudNum.Value.ToString();
            }
        }

        [Category("YAN Appearance"), Description("indicates the amount to increment or decrement on each button click.")]
        public decimal Increment { get => _nudNum.Increment; set => _nudNum.Increment = value; }

        [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
        public int BorderSize
        {
            get => _borderSize;
            set
            {
                value = Max(0, value);
                if (_borderSize != value)
                {
                    _borderSize = value;
                    UpdateNudRegion();
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("This property allows you to add rounded corners to the control.")]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                value = Max(0, value);
                if (_borderRadius != value)
                {
                    _borderRadius = value;
                    UpdateRegion();
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("Indicates the number of decimal places to display.")]
        public int DecimalPlaces
        {
            get => _nudNum.DecimalPlaces;
            set
            {
                if (_nudNum.DecimalPlaces != value)
                {
                    _nudNum.DecimalPlaces = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("When this property is true, the underline added to text.")]
        public bool UnderlinedStyle
        {
            get => _is_UnderlinedStyle;
            set
            {
                if (_is_UnderlinedStyle != value)
                {
                    _is_UnderlinedStyle = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("Indicates whether the thousands separator will be inserted between every three decimal digits.")]
        public bool ThousandsSeparator
        {
            get => _nudNum.ThousandsSeparator;
            set
            {
                if (_nudNum.ThousandsSeparator != value)
                {
                    _nudNum.ThousandsSeparator = value;
                    Invalidate();
                }
            }
        }

        //event
        [Category("YAN Event"), Description("Event raised when the value of the Val property is changed on Control.")]
        public event EventHandler ValueChanged;
        #endregion

        #region Overridden
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor = value;
                _nudNum.BackColor = value;
            }
        }

        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                base.ForeColor = value;
                _nudNum.ForeColor = value;
            }
        }

        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                _nudNum.Font = value;
                if (DesignMode)
                {
                    UpdateHCtrl();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            var borderRadius = GetBorderRadius();
            var borderSize = GetBorderSize();
            using var penBorder = new Pen(_is_Focus ? BorderFocusColor : _borderColor, borderSize);
            if (borderRadius > 1)
            {
                var rectBorderSmooth = ClientRectangle;
                var smoothSize = borderSize > 0 ? borderSize : 1;
                using var pathBorderSmooth = RoundedRect(rectBorderSmooth, borderRadius);
                using var pathBorder = RoundedRect(Inflate(rectBorderSmooth, -borderSize, -borderSize), borderRadius - borderSize);
                using var penBorderSmooth = new Pen(Parent?.BackColor ?? BackColor, smoothSize);
                graphics.SmoothingMode = AntiAlias;
                penBorder.Alignment = Center;
                // draw border smoothing
                graphics.DrawPath(penBorderSmooth, pathBorderSmooth);
                if (_is_UnderlinedStyle)
                {
                    // draw border
                    graphics.SmoothingMode = None;
                    if (borderSize >= 1)
                    {
                        graphics.DrawLine(penBorder, 0, Height - 1, Width, Height - 1);
                    }
                }
                else if (pathBorder != null)
                {
                    // draw border
                    graphics.DrawPath(penBorder, pathBorder);
                }
            }
            else
            {
                penBorder.Alignment = Inset;
                if (_is_UnderlinedStyle)
                {
                    graphics.DrawLine(penBorder, 0, Height - 1, Width, Height - 1);
                }
                else if (borderSize >= 1)
                {
                    graphics.DrawRectangle(penBorder, 0, 0, Width - 0.5f, Height - 0.5f);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
            if (DesignMode)
            {
                UpdateHCtrl();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateHCtrl();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            // the border smoothing is drawn with the parent back color
            Invalidate();
        }
        #endregion

        #region Events
        // Raises the enter event
        private void Nud_Enter(object sender, EventArgs e)
        {
            _is_Focus = true;
            _nudNum.Select(0, _nudNum.Text.Length);
            Invalidate();
        }

        // Raises the leave event
        private void Nud_Leave(object sender, EventArgs e)
        {
            _is_Focus = false;
            if (string.IsNullOrWhiteSpace(_nudNum.Text))
            {
                _nudNum.Text = _nudNum.Value.ToString();
            }
            Invalidate();
        }

        // Raises the key down event
        private void Nud_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e);
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
            }
        }

        // Raises the key press event
        private void Nud_KeyPress(object sender, KeyPressEventArgs e) => OnKeyPress(e);

        // Raises the value changed event and keeps the String field in sync with the value
        private void Nud_ValueChanged(object sender, EventArgs e)
        {
            String = _nudNum.Value.ToString();
            ValueChanged?.Invoke(sender, e);
        }

        // Update the rounded region of the numeric up-down when its size changes
        private void Nud_SizeChanged(object sender, EventArgs e) => UpdateNudRegion();
        #endregion

        #region Methods
        // Get the border radius that fits the current size (the configured value is never changed)
        private int GetBorderRadius() => (int)EffectiveRadius(ClientRectangle, _borderRadius);

        // Get the border size that fits the current size (the configured value is never changed)
        private int GetBorderSize() => Max(0, Min(_borderSize, Min(Width, Height) / 2));

        // Update the region of the control when its size or radius changes, never while painting
        private void UpdateRegion()
        {
            var borderRadius = GetBorderRadius();
            if (borderRadius > 1)
            {
                using var pathRegion = RoundedRect(ClientRectangle, borderRadius);
                SetRegion(this, pathRegion);
            }
            else
            {
                SetRegion(this, (Region)null);
            }
            UpdateNudRegion();
        }

        // Update the rounded region of the numeric up-down, only needed when the corners of the control are big enough to cut it
        private void UpdateNudRegion()
        {
            if (_nudNum == null)
            {
                return;
            }
            if (GetBorderRadius() > 15)
            {
                using var pathNum = RoundedRect(_nudNum.ClientRectangle, GetBorderSize() * 2);
                SetRegion(_nudNum, pathNum);
            }
            else
            {
                SetRegion(_nudNum, (Region)null);
            }
        }

        // Update the height of control when changed font display
        private void UpdateHCtrl()
        {
            _nudNum.MinimumSize = new Size(0, MeasureText("0", Font).Height + 1);
            Height = _nudNum.Height + Padding.Top + Padding.Bottom;
        }
        #endregion
    }
}
