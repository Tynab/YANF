using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Math;
using static System.Windows.Forms.TextRenderer;

namespace YANF.Control;

[DefaultEvent("StringChanged")]
public class YANTxt : UserControl
{
    #region Fields
    private Color _borderColor = MediumSlateBlue;
    private Color _placeholderColor = DarkGray;
    private string _placeholderText = null;
    private int _borderSize = 2;
    private int _borderRadius = 0;
    private bool _is_UnderlinedStyle = false;
    private bool _is_Focus = false;
    private bool _is_Placeholder = false;
    private bool _is_PasswordChar = false;
    private readonly TextBox _txtText;
    #endregion

    #region Constructors
    public YANTxt()
    {
        _txtText = new TextBox();
        SuspendLayout();
        // textbox text
        _txtText.BackColor = White;
        _txtText.BorderStyle = BorderStyle.None;
        _txtText.TextAlign = HorizontalAlignment.Center;
        _txtText.Dock = DockStyle.Fill;
        _txtText.Location = new Point(10, 7);
        _txtText.Size = new Size(180, 18);
        _txtText.Font = new Font(Font.Name, 11f);
        _txtText.MouseEnter += Txt_MouseEnter;
        _txtText.MouseLeave += Txt_MouseLeave;
        _txtText.Enter += Txt_Enter;
        _txtText.Leave += Txt_Leave;
        _txtText.KeyDown += Txt_KeyDown;
        _txtText.KeyPress += Txt_KeyPress;
        _txtText.KeyUp += Txt_KeyUp;
        _txtText.TextChanged += Txt_TextChanged;
        // user control
        Controls.Add(_txtText);
        ForeColor = DimGray;
        BackColor = White;
        AutoScaleMode = AutoScaleMode.None;
        Size = new Size(200, 30);
        Padding = new Padding(10, 7, 10, 7);
        Font = new Font(Font.Name, 11f);
        Resize += Ctrl_Resize;
        // base
        ResumeLayout();
    }
    #endregion

    #region Properties
    [Category("YAN Appearance"), Description("Indicates how the text should be aligned for edit controls.")]
    public HorizontalAlignment TextAlign
    {
        get => _txtText.TextAlign;
        set
        {
            _txtText.TextAlign = value;
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
    public Color BorderFocusColor { get; set; } = HotPink;

    [Category("YAN Appearance"), Description("The color of the placeholder text.")]
    public Color PlaceholderColor
    {
        get => _placeholderColor;
        set
        {
            _placeholderColor = value;
            if (_is_Placeholder)
            {
                _txtText.ForeColor = value;
            }
        }
    }

    [Category("YAN Appearance"), Description("The text associated with the control.")]
    public string String
    {
        get => _is_Placeholder ? null : _txtText.Text;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _txtText.Text = value;
                SetPlaceholder();
            }
            else
            {
                RemovePlaceholder();
                _txtText.Text = value;
            }
        }
    }

    [Category("YAN Appearance"), Description("The text that is displayed when the control has no text and does not have the focus.")]
    public string PlaceholderText
    {
        get => _placeholderText;
        set
        {
            _placeholderText = value;
            _txtText.Text = null;
            SetPlaceholder();
        }
    }

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

    [Category("YAN Appearance"), Description("Specifies the maximum number of characters that can be entered into the edit control.")]
    public int MaxLength { get => _txtText.MaxLength; set => _txtText.MaxLength = value; }

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

    [Category("YAN Appearance"), Description("Indicates the character to display for password input for single-line edit controls.")]
    public bool PasswordChar
    {
        get => _is_PasswordChar;
        set
        {
            _is_PasswordChar = value;
            if (!_is_Placeholder)
            {
                _txtText.UseSystemPasswordChar = value;
            }
        }
    }

    [Category("YAN Appearance"), Description("Control whether the text of the edit control can span more than one line.")]
    public bool Multiline { get => _txtText.Multiline; set => _txtText.Multiline = value; }

    // Event
    [Category("YAN Event"), Description("Event raised when the value of the Txt property is changed on Control.")]
    public event EventHandler StringChanged;
    #endregion

    #region Overridden
    // Background color
    public override Color BackColor
    {
        get => base.BackColor;
        set
        {
            base.BackColor = value;
            _txtText.BackColor = value;
        }
    }

    // Foreground color
    public override Color ForeColor
    {
        get => base.ForeColor;
        set
        {
            base.ForeColor = value;
            _txtText.ForeColor = value;
        }
    }

    // Font display
    public override Font Font
    {
        get => base.Font;
        set
        {
            base.Font = value;
            _txtText.Font = value;
            if (DesignMode)
            {
                UpdateHCtrl();
            }
        }
    }

    // One paint
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        if (_borderRadius > 1)
        {
            var rectBorderSmooth = ClientRectangle;
            using var pathBorderSmooth = GetFigurePath(rectBorderSmooth, _borderRadius);
            using var pathBorder = GetFigurePath(Inflate(rectBorderSmooth, -_borderSize, -_borderSize), _borderRadius - _borderSize);
            using var penBorderSmooth = new Pen(Parent.BackColor, _borderSize > 0 ? _borderSize : 1);
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
    // Raises the mouse enter event
    private void Txt_MouseEnter(object sender, EventArgs e) => OnMouseEnter(e);

    // Raises the mouse leave event
    private void Txt_MouseLeave(object sender, EventArgs e) => OnMouseLeave(e);

    // Raises the enter event
    private void Txt_Enter(object sender, EventArgs e)
    {
        _is_Focus = true;
        _txtText.Select(0, _txtText.Text.Length);
        Invalidate();
        RemovePlaceholder();
    }

    // Raises the leave event
    private void Txt_Leave(object sender, EventArgs e)
    {
        _is_Focus = false;
        Invalidate();
        SetPlaceholder();
    }

    // Raises the key press event
    private void Txt_KeyPress(object sender, KeyPressEventArgs e) => OnKeyPress(e);

    // Raises the key down event
    private void Txt_KeyDown(object sender, KeyEventArgs e)
    {
        OnKeyDown(e);
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
        }
    }

    // Raises the key up event
    private void Txt_KeyUp(object sender, KeyEventArgs e) => OnKeyUp(e);

    // Raises the text changed event
    private void Txt_TextChanged(object sender, EventArgs e)
    {
        if (StringChanged != null)
        {
            StringChanged.Invoke(sender, e);
        }
    }

    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e)
    {
        var minSize = Width > Height ? Height : Width;
        _borderRadius = Min(_borderRadius, minSize / 2);
        _borderSize = Min(_borderSize, minSize / 2);
    }
    #endregion

    #region Methods
    // Add placeholder text to the control
    private void SetPlaceholder()
    {
        if (string.IsNullOrEmpty(_txtText.Text) && !string.IsNullOrEmpty(_placeholderText))
        {
            _is_Placeholder = true;
            _txtText.Text = _placeholderText;
            _txtText.ForeColor = _placeholderColor;
            if (_is_PasswordChar)
            {
                if (Created)
                {
                    _ = BeginInvoke(new Action(() => _txtText.UseSystemPasswordChar = false));
                }
                else
                {
                    _txtText.UseSystemPasswordChar = false;
                }
            }
        }
    }

    // Remove placeholder text to the control
    private void RemovePlaceholder()
    {
        if (_is_Placeholder && !string.IsNullOrEmpty(_placeholderText))
        {
            _is_Placeholder = false;
            _txtText.Text = null;
            _txtText.ForeColor = ForeColor;
            if (_is_PasswordChar)
            {
                _txtText.UseSystemPasswordChar = true;
            }
        }
    }

    // Set rounded region to the control
    private void SetTextRoundedRegion()
    {
        var client = _txtText.ClientRectangle;
        _txtText.Region = Multiline ? new Region(GetFigurePath(client, _borderRadius - _borderSize)) : new Region(GetFigurePath(client, _borderSize * 2));
    }

    // Update the height of control when changed font display
    private void UpdateHCtrl()
    {
        if (!_txtText.Multiline)
        {
            _txtText.Multiline = true;
            _txtText.MinimumSize = new Size(0, MeasureText("Text", Font).Height + 1);
            _txtText.Multiline = false;
            Height = _txtText.Height + Padding.Top + Padding.Bottom;
        }
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
