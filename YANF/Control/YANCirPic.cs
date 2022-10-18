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

namespace YANF.Control;

public class YANCirPic : PictureBox
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
    #endregion

    #region Constructors
    public YANCirPic()
    {
        // property
        Size = new Size(100, 100);
        SizeMode = StretchImage;
        // event
        Resize += Ctrl_Resize;
    }
    #endregion

    #region Properties
    [Category("YAN Appearance"), Description("The color of the top border gradient.")]
    public Color BorderTopColor
    {
        get => _borderTopColor;
        set
        {
            _borderTopColor = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("The color of the bottom border gradient.")]
    public Color BorderBottomColor
    {
        get => _borderBottomColor;
        set
        {
            _borderBottomColor = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("The color of the top gradient.")]
    public Color TopColor
    {
        get => _topColor;
        set
        {
            _topColor = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("The color of the bottom gradient.")]
    public Color BottomColor
    {
        get => _bottomColor;
        set
        {
            _bottomColor = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("Sets and returns the line style used to draw the border line of the control.")]
    public DashStyle BorderLineStyle
    {
        get => _borderLineStyle;
        set
        {
            _borderLineStyle = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("Sets the style of element of border.")]
    public DashCap BorderCapStyle
    {
        get => _borderCapStyle;
        set
        {
            _borderCapStyle = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("Angle of the gradient of border.")]
    public float BorderAngle
    {
        get => _borderAngle;
        set
        {
            if (value is >= 0 and <= 360)
            {
                _borderAngle = value;
                Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("Angle of the gradient.")]
    public float Angle
    {
        get => _angle;
        set
        {
            if (value is >= 0 and <= 360)
            {
                _angle = value;
                Invalidate();
            }
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
    #endregion

    #region Overridden
    // On resize
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Size = new Size(Width, Width);
    }

    // On paint
    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        using (var brush = new LinearGradientBrush(ClientRectangle, _topColor, _bottomColor, _angle))
        {
            graphics.FillRectangle(brush, ClientRectangle);
        }
        base.OnPaint(e);
        var rectContourSmooth = Inflate(ClientRectangle, -1, -1);
        var rectBorder = Inflate(rectContourSmooth, -_borderSize, -_borderSize);
        using var borderGColor = new LinearGradientBrush(rectBorder, _borderTopColor, _borderBottomColor, _borderAngle);
        using var pathRegion = new GraphicsPath();
        using var penSmooth = new Pen(Parent.BackColor, _borderSize > 0 ? _borderSize * 3 : 1);
        using var penBorder = new Pen(borderGColor, _borderSize);
        graphics.SmoothingMode = AntiAlias;
        penBorder.DashStyle = _borderLineStyle;
        penBorder.DashCap = _borderCapStyle;
        pathRegion.AddEllipse(rectContourSmooth);
        Region = new Region(pathRegion);
        // drawing
        graphics.DrawEllipse(penSmooth, rectContourSmooth);
        if (_borderSize > 0)
        {
            graphics.DrawEllipse(penBorder, rectBorder);
        }
    }
    #endregion

    #region Events
    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e)
    {
        var minSize = Width > Height ? Height : Width;
        _borderSize = Min(_borderSize, minSize / 2);
    }
    #endregion
}
