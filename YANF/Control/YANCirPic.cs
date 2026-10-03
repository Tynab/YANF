using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.DashCap;
using static System.Drawing.Drawing2D.DashStyle;
using static System.Math;
using static System.Windows.Forms.PictureBoxSizeMode;
using static YANF.Control.YANPaint;
using static YANF.Script.YANShape;

namespace YANF.Control;

using WinControl = System.Windows.Forms.Control;

/// <summary>
/// A round picture: the image (placed by <see cref="SizeMode"/>) on a gradient, in a circle with a gradient border. Around the
/// circle the parent's own pixels show (2.0: a gradient, an image, what its Paint handlers draw), with an anti-aliased edge. 1.x
/// cut the control with a region, so whatever lay behind the corners showed through them, sibling controls included, and hid the
/// jagged edge with a ring of Parent.BackColor. 2.0 paints the parent there instead and, as for a transparent BackColor, not the
/// sibling controls that overlap the control: a layout where the circle overlaps another control shows the parent in its
/// corners. Clicks around the circle go to the parent, as with the 1.x region. With a <see cref="PictureBox.BorderStyle"/> other
/// than None, whose frame Windows draws outside the client area, the control is still cut to the circle with a region, which
/// hides the frame as in 1.x.
/// </summary>
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
    // the region is the library's (a BorderStyle frame), not one the application set
    private bool _is_RegionSet = false;
    // the circle that takes the clicks (the 1.x region): the square inset by CIRCLE_INSET, with the largest radius
    private readonly YANBorder _hitCircle;
    // the circle is inset by one 96-dpi pixel from the square, as the 1.x region was
    private const int CIRCLE_INSET = 1;
    // without a border, 1.x smoothed the edge with a ring of half a 96-dpi pixel inside the region
    private const float EDGE_RING = 0.5f;
    #endregion

    #region Constructors
    public YANCirPic()
    {
        _hitCircle = new YANBorder(this, 0, int.MaxValue);
        OptionDisplay();
        // the parent is painted around the circle: repaint when the control moves or when the parent changes behind it
        FollowParent(this);
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

    /// <summary>
    /// Gets or sets the width of the border in 96-dpi pixels: it is scaled with the DPI of the control (2.0), and clamped to the
    /// size of the circle when painted.
    /// </summary>
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

    /// <summary>
    /// Gets or sets how the image is placed in the control, as <see cref="PictureBox.SizeMode"/>. Redeclared (2.0) for its designer
    /// default only: the constructor sets <see cref="PictureBoxSizeMode.StretchImage"/>, so the designer now writes every other
    /// mode, <see cref="PictureBoxSizeMode.Normal"/> included (1.x did not write Normal, which the constructor then replaced).
    /// </summary>
    [Category("Behavior"), Description("Controls how the image is placed and sized in the circle. The default is StretchImage.")]
    [DefaultValue(StretchImage)]
    [Localizable(true)]
    [RefreshProperties(RefreshProperties.Repaint)]
    public new PictureBoxSizeMode SizeMode
    {
        get => base.SizeMode;
        set => base.SizeMode = value;
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

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // a new BorderStyle recreates the handle with its frame
        UpdateRegion();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // an opaque gradient fills the circle and the parent is laid around it: nothing of the background (the BackColor, or the
        // parent for a transparent one) would show
        if (_topColor.A == 255 && _bottomColor.A == 255 && GetCircle(out _, out _, out _))
        {
            return;
        }
        base.OnPaintBackground(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        var rectSurface = GetSquare();
        if (rectSurface.Width > 0)
        {
            // flipped tiles keep GDI+ from wrapping the end color onto the first row or column
            using var brush = new LinearGradientBrush(rectSurface, _topColor, _bottomColor, _angle)
            {
                WrapMode = WrapMode.TileFlipXY
            };
            graphics.FillRectangle(brush, rectSurface);
        }
        base.OnPaint(e);
        if (!GetCircle(out var rectCircle, out var rectBorder, out var borderSize))
        {
            return;
        }
        // around the circle: the parent's own pixels, anti-aliased along the edge
        using (var pathCircle = new GraphicsPath())
        {
            pathCircle.AddEllipse(rectCircle);
            PaintParentOutside(this, e, pathCircle);
        }
        if (borderSize <= 0)
        {
            return;
        }
        // draw border: the gradient border is drawn in the system frame color in high contrast mode
        using Brush borderGColor = IsHighContrast ? new SolidBrush(SystemColors.WindowFrame) : new LinearGradientBrush(rectBorder, _borderTopColor, _borderBottomColor, _borderAngle)
        {
            WrapMode = WrapMode.TileFlipXY
        };
        using var penBorder = new Pen(borderGColor, borderSize)
        {
            DashStyle = _borderLineStyle,
            DashCap = _borderCapStyle
        };
        var state = graphics.Save();
        try
        {
            SetCrisp(graphics);
            graphics.DrawEllipse(penBorder, rectBorder);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    // The clicks around the circle go to the parent (HTTRANSPARENT), as with the 1.x region
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        _hitCircle.Bounds = GetContour();
        _ = _hitCircle.HitTest(ref m);
    }

    protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
    {
        base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
        // the circle and the region are rebuilt for the new DPI
        _hitCircle.Reset();
        UpdateRegion();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hitCircle.Dispose();
        }
        base.Dispose(disposing);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Lays the parent's own pixels over what lies outside <paramref name="shape"/> (client coordinates), anti-aliased along its
    /// edge: what a control whose base class paints its whole client area (a picture, a button face) shows around its round
    /// shape. The parent is painted into a bitmap over the area to paint (see <see cref="YANPaint.PaintParent(WinControl, Graphics, Rectangle)"/>)
    /// and filled through the complement of the shape with a texture brush, in the pixel model of <see cref="YANPaint.SetCrisp"/>.
    /// The state of the graphics is restored afterwards. Shared with <see cref="YANBtn"/>.
    /// </summary>
    internal static void PaintParentOutside(WinControl ctrl, PaintEventArgs e, GraphicsPath shape)
    {
        var rectClient = ctrl.ClientRectangle;
        var clip = Rectangle.Intersect(rectClient, e.ClipRectangle);
        if (shape == null || clip.Width <= 0 || clip.Height <= 0)
        {
            return;
        }
        // an opaque bitmap: GDI drawing of the parent (TextRenderer) keeps its pixels opaque
        using var bmpParent = new Bitmap(clip.Width, clip.Height, PixelFormat.Format24bppRgb);
        using (var graphicsParent = Graphics.FromImage(bmpParent))
        {
            graphicsParent.TranslateTransform(-clip.X, -clip.Y);
            PaintParent(ctrl, graphicsParent, clip);
        }
        // the client area, one pixel wider so that its edges never cut the fill, minus the shape (alternate fill mode)
        using var pathOutside = new GraphicsPath();
        pathOutside.AddRectangle(Rectangle.Inflate(rectClient, 1, 1));
        pathOutside.AddPath(shape, false);
        using var brushParent = new TextureBrush(bmpParent);
        brushParent.TranslateTransform(clip.X, clip.Y);
        var graphics = e.Graphics;
        var state = graphics.Save();
        try
        {
            graphics.IntersectClip(clip);
            SetCrisp(graphics);
            graphics.FillPath(brushParent, pathOutside);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

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

    // Get the contour of the circle: the square inset by one pixel (the 1.x region)
    private Rectangle GetContour()
    {
        var inset = LogicalToDevice(this, CIRCLE_INSET);
        return Rectangle.Inflate(GetSquare(), -inset, -inset);
    }

    // Get the painted circle, the centre line of the border and the border width in device pixels, with the 1.x geometry: the ring
    // inside the contour, where 1.x painted Parent.BackColor over the picture, shows the parent (half the border rounded up to whole
    // pixels, as 1.x's pen covered them, so that an odd border stays crisp), and the border lies inside it (clamped so that its
    // centre line keeps a width). False when there is no circle
    private bool GetCircle(out RectangleF rectCircle, out RectangleF rectBorder, out int borderSize)
    {
        var rectContour = GetContour();
        if (rectContour.Width <= 0 || rectContour.Height <= 0)
        {
            rectCircle = RectangleF.Empty;
            rectBorder = RectangleF.Empty;
            borderSize = 0;
            return false;
        }
        var dpi = GetDpi(this);
        borderSize = Max(0, Min(LogicalToDevice(_borderSize, dpi), (rectContour.Width - 1) / 2));
        // the ring of an odd border is half a pixel wider: keep the centre line of the border a width
        if (borderSize % 2 == 1 && 2 * borderSize + 2 > rectContour.Width)
        {
            borderSize--;
        }
        var ring = borderSize > 0 ? (borderSize + 1) / 2 : LogicalToDevice(EDGE_RING, dpi);
        rectCircle = RectangleF.Inflate(rectContour, -ring, -ring);
        // the pen is centred between the ring and the circle inside the border
        var inset = ring + borderSize / 2f;
        rectBorder = RectangleF.Inflate(rectContour, -inset, -inset);
        return true;
    }

    // Update the region of a control with a BorderStyle frame when its size, frame or DPI changes, never while painting: Windows
    // draws the frame outside the client area, where the parent cannot be painted, so the window is cut to the circle (the 1.x
    // region). Without a frame there is no region, and a region that the application set is left alone
    private void UpdateRegion()
    {
        if (BorderStyle != BorderStyle.None)
        {
            var rectContour = GetContour();
            if (rectContour.Width > 0 && rectContour.Height > 0)
            {
                // the region is in window coordinates: the client area starts inside the frame, which is as wide on every side
                rectContour.Offset((Width - ClientSize.Width) / 2, (Height - ClientSize.Height) / 2);
                using var pathRegion = new GraphicsPath();
                pathRegion.AddEllipse(rectContour);
                SetRegion(this, pathRegion);
                _is_RegionSet = true;
                return;
            }
        }
        if (_is_RegionSet)
        {
            _is_RegionSet = false;
            SetRegion(this, (Region)null);
        }
    }
    #endregion
}
