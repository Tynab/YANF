using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.DashCap;
using static System.Drawing.Drawing2D.DashStyle;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Math;
using static System.Windows.Forms.PictureBoxSizeMode;
using static YANF.Script.YANShape;

namespace YANF.Control;

[ToolboxBitmap(typeof(PictureBox))]
public partial class YANCirPic : PictureBox
{
    #region Fields
    private Color _borderTopColor = RoyalBlue;
    private Color _borderBottomColor = HotPink;
    private Color _topColor = RoyalBlue;
    private Color _bottomColor = HotPink;
    private DashStyle _borderLineStyle = Solid;
    private DashCap _borderCapStyle = Flat;
    private float _borderAngle = 50f;
    private float _angle;
    private int _borderSize = 2;
    private bool _is_Squaring = false;
    #endregion

    #region Constructors
    public YANCirPic()
    {
        OptionDisplay();
    }
    #endregion

    #region Properties
    [Category("YAN Appearance"), Description("The color of the top border gradient.")]
    [DefaultValue(typeof(Color), "RoyalBlue")]
    public Color BorderTopColor
    {
        get => _borderTopColor;
        set
        {
            if (_borderTopColor != value)
            {
                _borderTopColor = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The color of the bottom border gradient.")]
    [DefaultValue(typeof(Color), "HotPink")]
    public Color BorderBottomColor
    {
        get => _borderBottomColor;
        set
        {
            if (_borderBottomColor != value)
            {
                _borderBottomColor = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The color of the top gradient.")]
    [DefaultValue(typeof(Color), "RoyalBlue")]
    public Color TopColor
    {
        get => _topColor;
        set
        {
            if (_topColor != value)
            {
                _topColor = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The color of the bottom gradient.")]
    [DefaultValue(typeof(Color), "HotPink")]
    public Color BottomColor
    {
        get => _bottomColor;
        set
        {
            if (_bottomColor != value)
            {
                _bottomColor = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The dash style of the border line of the control.")]
    [DefaultValue(DashStyle.Solid)]
    public DashStyle BorderLineStyle
    {
        get => _borderLineStyle;
        set
        {
            if (_borderLineStyle != value)
            {
                _borderLineStyle = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The cap style of the dashes of the border line.")]
    [DefaultValue(DashCap.Flat)]
    public DashCap BorderCapStyle
    {
        get => _borderCapStyle;
        set
        {
            if (_borderCapStyle != value)
            {
                _borderCapStyle = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The angle, in degrees from 0 to 360, of the border gradient.")]
    [DefaultValue(50f)]
    public float BorderAngle
    {
        get => _borderAngle;
        set
        {
            if (value is >= 0 and <= 360 && _borderAngle != value)
            {
                _borderAngle = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The angle, in degrees from 0 to 360, of the gradient.")]
    [DefaultValue(0f)]
    public float Angle
    {
        get => _angle;
        set
        {
            if (value is >= 0 and <= 360 && _angle != value)
            {
                _angle = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
    [DefaultValue(2)]
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
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        // keep the control square, unless a dock layout owns its size
        if (!_is_Squaring && Dock == DockStyle.None && Height != Width)
        {
            _is_Squaring = true;
            try
            {
                Size = new Size(Width, Width);
            }
            finally
            {
                _is_Squaring = false;
            }
        }
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        var rectSurface = GetSquare();
        if (rectSurface.Width > 0)
        {
            using var brush = new LinearGradientBrush(rectSurface, _topColor, _bottomColor, _angle);
            graphics.FillRectangle(brush, rectSurface);
        }
        base.OnPaint(e);
        var rectContourSmooth = Inflate(rectSurface, -1, -1);
        if (rectContourSmooth.Width <= 0)
        {
            return;
        }
        var borderSize = GetBorderSize(rectContourSmooth);
        var rectBorder = Inflate(rectContourSmooth, -borderSize, -borderSize);
        // the gradient border is drawn in the system frame color in high contrast mode
        using Brush borderGColor = SystemInformation.HighContrast ? new SolidBrush(SystemColors.WindowFrame) : new LinearGradientBrush(rectBorder, _borderTopColor, _borderBottomColor, _borderAngle);
        using var penSmooth = new Pen(Parent?.BackColor ?? BackColor, borderSize > 0 ? borderSize * 3 : 1);
        using var penBorder = new Pen(borderGColor, borderSize);
        graphics.SmoothingMode = AntiAlias;
        penBorder.DashStyle = _borderLineStyle;
        penBorder.DashCap = _borderCapStyle;
        // drawing
        graphics.DrawEllipse(penSmooth, rectContourSmooth);
        if (borderSize > 0)
        {
            graphics.DrawEllipse(penBorder, rectBorder);
        }
    }
    #endregion

    #region Methods
    // Option display
    private void OptionDisplay()
    {
        ResizeRedraw = true;
        Size = new Size(100, 100);
        SizeMode = StretchImage;
    }

    // Get the largest square centred in the client area, the circle is always painted inside it
    private Rectangle GetSquare()
    {
        var rectClient = ClientRectangle;
        var side = Min(rectClient.Width, rectClient.Height);
        return new Rectangle(rectClient.X + (rectClient.Width - side) / 2, rectClient.Y + (rectClient.Height - side) / 2, side, side);
    }

    // Get the border size that keeps the border ellipse non-empty (the configured value is never changed)
    private int GetBorderSize(Rectangle rectContour) => Max(0, Min(_borderSize, (rectContour.Width - 1) / 2));

    // Update the circular region of the control when its size changes, never while painting
    private void UpdateRegion()
    {
        var rectContourSmooth = Inflate(GetSquare(), -1, -1);
        if (rectContourSmooth.Width <= 0)
        {
            SetRegion(this, (Region)null);
            return;
        }
        using var pathRegion = new GraphicsPath();
        pathRegion.AddEllipse(rectContourSmooth);
        SetRegion(this, pathRegion);
    }
    #endregion
}