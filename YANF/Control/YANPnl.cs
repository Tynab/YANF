using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Math;
using static System.Windows.Forms.ControlStyles;

namespace YANF.Control;

public class YANPnl : Panel
{
    #region Fields
    private Color _borderColor = RoyalBlue;
    private int _borderSize = 1;
    private int _borderRadius = 20;
    #endregion

    #region Constructors
    public YANPnl()
    {
        SetStyle(Selectable, false);
        // property
        TabStop = false;
        TabIndex = 0;
        // event
        Resize += Ctrl_Resize;
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
    // On paint
    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        var x = e.ClipRectangle.Left;
        var y = e.ClipRectangle.Top;
        var width = e.ClipRectangle.Width - _borderSize - 1;
        var height = e.ClipRectangle.Height - _borderSize - 1;
        var path = new GraphicsPath();
        path.AddLine(x + _borderRadius, y, x + width - (_borderRadius * 2), y);
        path.AddArc(x + width - (_borderRadius * 2), y, _borderRadius * 2, _borderRadius * 2, 270, 90);
        path.AddLine(x + width, y + _borderRadius, x + width, y + height - (_borderRadius * 2));
        path.AddArc(x + width - (_borderRadius * 2), y + height - (_borderRadius * 2), _borderRadius * 2, _borderRadius * 2, 0, 90);
        path.AddLine(x + width - (_borderRadius * 2), y + height, x + _borderRadius, y + height);
        path.AddArc(x, y + height - (_borderRadius * 2), _borderRadius * 2, _borderRadius * 2, 90, 90);
        path.AddLine(x, y + height - (_borderRadius * 2), x, y + _borderRadius);
        path.AddArc(x, y, _borderRadius * 2, _borderRadius * 2, 180, 90);
        path.CloseFigure();
        using var penBorder = new Pen(_borderColor, _borderSize);
        graphics.DrawPath(penBorder, path);
        base.OnPaint(e);
    }
    #endregion

    #region Events
    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e)
    {
        var minSize = Width > Height ? Height : Width;
        _borderRadius = Min(_borderRadius, minSize / 2);
        _borderSize = Min(_borderSize, minSize / 2);
    }
    #endregion
}
