using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Windows.Forms.TextRenderer;
using static YANF.Script.YANCommon;

namespace YANF.Control;

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
        // user control
        Controls.Add(_nudNum);
        ForeColor = DimGray;
        BackColor = White;
        ThousandsSeparator = true;
        AutoScaleMode = AutoScaleMode.None;
        String = Value.ToString();
        Size = new Size(200, 30);
        Padding = new Padding(10, 7, 10, 7);
        Font = new Font(Font.Name, 11f);
        Resize += Ctrl_Resize;
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
            _nudNum.TextAlign = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("This property specifies the color of the border around the control.")]
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
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
            if (value > Value)
            {
                Value = value;
                String = Value.ToString();
                _nudNum.Value = value;
            }
            _nudNum.Minimum = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("Indicates the maximum value for the numeric up-down control.")]
    public decimal Maximum
    {
        get => _nudNum.Maximum;
        set
        {
            if (value < Value)
            {
                Value = value;
                String = Value.ToString();
                _nudNum.Value = value;
            }
            _nudNum.Maximum = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("The current value of the numeric up-down control.")]
    public decimal Value
    {
        get => _nudNum.Value;
        set
        {
            _nudNum.Value = value < Minimum ? Minimum : value > Maximum ? Maximum : value;
            String = value.ToString();
            Invalidate();
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
            _borderSize = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("This property allows you to add rounded corners to the control.")]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            _borderRadius = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("Indicates the number of decimal places to display.")]
    public int DecimalPlaces
    {
        get => _nudNum.DecimalPlaces;
        set
        {
            _nudNum.DecimalPlaces = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("When this property is true, the underline added to text.")]
    public bool UnderlinedStyle
    {
        get => _is_UnderlinedStyle;
        set
        {
            _is_UnderlinedStyle = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("Indicates whether the thousands separator will be inserted between every three decimal digits.")]
    public bool ThousandsSeparator
    {
        get => _nudNum.ThousandsSeparator;
        set
        {
            _nudNum.ThousandsSeparator = value;
            Invalidate();
        }
    }

    //event
    [Category("YAN Event"), Description("Event raised when the value of the Val property is changed on Control.")]
    public event EventHandler ValueChanged;
    #endregion

    #region Overridden
    // Background color
    public override Color BackColor
    {
        get => base.BackColor;
        set
        {
            base.BackColor = value;
            _nudNum.BackColor = value;
        }
    }

    // Foreground color
    public override Color ForeColor
    {
        get => base.ForeColor;
        set
        {
            base.ForeColor = value;
            _nudNum.ForeColor = value;
        }
    }

    // Font display
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

    // On paint
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        if (_borderRadius > 1)
        {
            var rectBorderSmooth = ClientRectangle;
            var smoothSize = _borderSize > 0 ? _borderSize : 1;
            using var pathBorderSmooth = GetFigurePath(rectBorderSmooth, _borderRadius);
            using var pathBorder = GetFigurePath(Inflate(rectBorderSmooth, -_borderSize, -_borderSize), _borderRadius - _borderSize);
            using var penBorderSmooth = new Pen(Parent.BackColor, smoothSize);
            using var penBorder = new Pen(_borderColor, _borderSize);
            Region = new Region(pathBorderSmooth);
            if (_borderRadius > 15)
            {
                SetTextRoundedRegion();
            }
            graphics.SmoothingMode = AntiAlias;
            penBorder.Alignment = Center;
            if (_is_Focus)
            {
                penBorder.Color = BorderFocusColor;
            }
            if (_is_UnderlinedStyle)
            {
                // draw border smoothing
                graphics.DrawPath(penBorderSmooth, pathBorderSmooth);
                // draw border
                graphics.SmoothingMode = None;
                graphics.DrawLine(penBorder, 0, Height - 1, Width, Height - 1);
            }
            else
            {
                // draw border smoothing
                graphics.DrawPath(penBorderSmooth, pathBorderSmooth);
                // draw border
                graphics.DrawPath(penBorder, pathBorder);
            }
        }
        else
        {
            using var penBorder = new Pen(_borderColor, _borderSize);
            Region = new Region(ClientRectangle);
            penBorder.Alignment = Inset;
            if (_is_Focus)
            {
                penBorder.Color = BorderFocusColor;
            }
            if (_is_UnderlinedStyle)
            {
                graphics.DrawLine(penBorder, 0, Height - 1, Width, Height - 1);
            }
            else
            {
                graphics.DrawRectangle(penBorder, 0, 0, Width - 0.5f, Height - 0.5f);
            }
        }
    }

    // On resize
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (DesignMode)
        {
            UpdateHCtrl();
        }
    }

    // On load
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        UpdateHCtrl();
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

    //raises the value changed event
    private void Nud_ValueChanged(object sender, EventArgs e)
    {
        if (ValueChanged != null)
        {
            ValueChanged.Invoke(sender, e);
        }
    }
    #endregion

    #region Event Handles
    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e)
    {
        var minSize = Width > Height ? Height : Width;
        _borderRadius = Miner(_borderRadius, minSize / 2);
        _borderSize = Miner(_borderSize, minSize / 2);
    }
    #endregion

    #region Methods
    // Set rounded region to the control
    private void SetTextRoundedRegion() => _nudNum.Region = new Region(GetFigurePath(_nudNum.ClientRectangle, _borderSize * 2));

    // Update the height of control when changed font display
    private void UpdateHCtrl()
    {
        _nudNum.MinimumSize = new Size(0, MeasureText("0", Font).Height + 1);
        Height = _nudNum.Height + Padding.Top + Padding.Bottom;
    }

    // Get path of figure
    private GraphicsPath GetFigurePath(RectangleF rectF, float rad)
    {
        var path = new GraphicsPath();
        var curveSize = rad * 2f;
        path.StartFigure();
        path.AddArc(rectF.X, rectF.Y, curveSize, curveSize, 180, 90);
        path.AddArc(rectF.Right - curveSize, rectF.Y, curveSize, curveSize, 270, 90);
        path.AddArc(rectF.Right - curveSize, rectF.Bottom - curveSize, curveSize, curveSize, 0, 90);
        path.AddArc(rectF.X, rectF.Bottom - curveSize, curveSize, curveSize, 90, 90);
        path.CloseFigure();
        return path;
    }
    #endregion
}
