using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.FlatStyle;

namespace YANF.Control;

public partial class YANBtn : Button
{
    #region Fields
    private Color _borderColor = PaleVioletRed;
    private int _borderSize = 0;
    private int _borderRadius = 20;
    #endregion

    #region Constructors
    public YANBtn()
    {
        OptionEvent();
        OptionDisplay();
    }
    #endregion

    #region Properties
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
    #endregion

    #region Overridden
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        var rectSurface = ClientRectangle;
        if (_borderRadius > 2)
        {
            using var pathSurface = GetFigurePath(rectSurface, _borderRadius);
            using var pathBorder = GetFigurePath(Inflate(rectSurface, -_borderSize, -_borderSize), _borderRadius - _borderSize);
            using var penSurface = new Pen(Parent.BackColor, _borderSize > 0 ? _borderSize : 2);
            using var penBorder = new Pen(_borderColor, _borderSize);
            graphics.SmoothingMode = AntiAlias;
            Region = new Region(pathSurface);
            // draw surface
            graphics.DrawPath(penSurface, pathSurface);
            // draw border
            if (_borderSize >= 1)
            {
                graphics.DrawPath(penBorder, pathBorder);
            }
        }
        else
        {
            graphics.SmoothingMode = None;
            Region = new Region(rectSurface);
            // draw border
            if (_borderSize >= 1)
            {
                using var penBorder = new Pen(_borderColor, _borderSize);
                penBorder.Alignment = Inset;
                graphics.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Parent.BackColorChanged += Container_BackColorChanged;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = Hand;
    }
    #endregion

    #region Methods
    // Option display
    private void OptionDisplay()
    {
        SetStyle(Selectable, false);
        TabStop = false;
        TabIndex = 0;
        FlatStyle = Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = MediumSlateBlue;
        ForeColor = White;
        Size = new Size(150, 40);
        Font = new Font(Font.Name, 10f);
    }

    // Option event
    private void OptionEvent() => Resize += Ctrl_Resize;

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