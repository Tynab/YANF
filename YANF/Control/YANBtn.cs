using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Math;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.FlatStyle;
using static YANF.Script.YANShape;

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
            if (_borderColor != value)
            {
                _borderColor = value;
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
            value = Max(0, value);
            if (_borderSize != value)
            {
                _borderSize = value;
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
    #endregion

    #region Overridden
    protected override Cursor DefaultCursor => Hand;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var graphics = e.Graphics;
        var rectSurface = ClientRectangle;
        var borderRadius = GetBorderRadius();
        var borderSize = GetBorderSize();
        if (borderRadius > 2)
        {
            using var pathSurface = RoundedRect(rectSurface, borderRadius);
            using var pathBorder = RoundedRect(Inflate(rectSurface, -borderSize, -borderSize), borderRadius - borderSize);
            using var penSurface = new Pen(Parent?.BackColor ?? BackColor, borderSize > 0 ? borderSize : 2);
            using var penBorder = new Pen(_borderColor, borderSize);
            graphics.SmoothingMode = AntiAlias;
            // draw surface
            graphics.DrawPath(penSurface, pathSurface);
            // draw border
            if (borderSize >= 1 && pathBorder != null)
            {
                graphics.DrawPath(penBorder, pathBorder);
            }
        }
        else
        {
            graphics.SmoothingMode = None;
            // draw border
            if (borderSize >= 1 && Width > 1 && Height > 1)
            {
                using var penBorder = new Pen(_borderColor, borderSize);
                penBorder.Alignment = Inset;
                graphics.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
            }
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRegion();
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        Invalidate();
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

    // Get the border radius that fits the current size, in whole pixels (the configured value is never changed)
    private int GetBorderRadius() => (int)EffectiveRadius(ClientRectangle, _borderRadius);

    // Get the border size that fits the current size (the configured value is never changed)
    private int GetBorderSize() => Max(0, Min(_borderSize, Min(Width, Height) / 2));

    // Update the region of the control when its size or radius changes, never while painting
    private void UpdateRegion()
    {
        var borderRadius = GetBorderRadius();
        if (borderRadius > 2)
        {
            using var pathRegion = RoundedRect(ClientRectangle, borderRadius);
            SetRegion(this, pathRegion);
        }
        else
        {
            SetRegion(this, (Region)null);
        }
    }
    #endregion
}