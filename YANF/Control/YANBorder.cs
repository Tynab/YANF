using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using YANF.Script;
using static System.Math;
using static YANF.Control.YANPaint;

namespace YANF.Control
{
    using WinControl = System.Windows.Forms.Control;

    /// <summary>
    /// The rounded shape and the border of a control, shared by composition (no public base class): the control keeps its own
    /// public BorderSize / BorderRadius properties, with their attributes and defaults, and forwards them here. Values are 96-dpi
    /// logical pixels; the shape is built for the owner's client rectangle (or <see cref="Bounds"/>) at the owner's DPI, cached
    /// until one of them changes, and painted anti-aliased over the painted parent (<see cref="YANPaint.PaintParent(WinControl, PaintEventArgs)"/>)
    /// instead of being cut out with a region. The configured values are never changed to fit: they are clamped when the shape is built.
    /// </summary>
    /// <example>
    /// <code>
    /// private readonly YANBorder _border;
    ///
    /// public YANBtn()
    /// {
    ///     _border = new YANBorder(this, 0, 20);         // the constructor defaults of BorderSize and BorderRadius
    ///     YANPaint.FollowParent(this);
    /// }
    ///
    /// [Category("YAN Appearance"), Description("..."), DefaultValue(20)]
    /// public int BorderRadius
    /// {
    ///     get => _border.Radius;
    ///     set
    ///     {
    ///         value = Max(0, value);
    ///         if (_border.Radius != value)
    ///         {
    ///             _border.Radius = value;
    ///             Invalidate();
    ///         }
    ///     }
    /// }
    ///
    /// protected override void OnPaintBackground(PaintEventArgs e)
    /// {
    ///     YANPaint.PaintParent(this, e);                // what shows through the rounded corners
    ///     _border.FillShape(e.Graphics, BackColor);     // the anti-aliased surface
    /// }
    ///
    /// protected override void OnPaint(PaintEventArgs e)
    /// {
    ///     base.OnPaint(e);
    ///     _border.DrawBorder(e.Graphics, YANPaint.Contrast(BorderColor, SystemColors.WindowFrame));
    /// }
    ///
    /// protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
    /// {
    ///     base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
    ///     _border.Reset();                              // rebuilt for the new DPI at the next paint
    /// }
    ///
    /// // only when the corners must let clicks through to the parent (1.x cut them off with a region)
    /// protected override void WndProc(ref Message m)
    /// {
    ///     base.WndProc(ref m);
    ///     _ = _border.HitTest(ref m);
    /// }
    ///
    /// protected override void Dispose(bool disposing)
    /// {
    ///     if (disposing)
    ///     {
    ///         _border.Dispose();
    ///     }
    ///     base.Dispose(disposing);
    /// }
    /// </code>
    /// A child window that the rounded corners would cut (the inner TextBox of YANTxt) still needs a region: build it from
    /// <see cref="Shape"/> or <see cref="DeviceRadius"/> when the size or the DPI changes, with <see cref="YANShape.SetRegion(WinControl, GraphicsPath)"/>.
    /// </example>
    internal sealed class YANBorder : IDisposable
    {
        #region Fields
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;
        private readonly WinControl _owner;
        private int _size;
        private int _radius;
        private Rectangle? _bounds;
        private GraphicsPath _shape;
        private GraphicsPath _line;
        private GraphicsPath _ring;
        private Rectangle _builtBounds;
        private int _builtDpi;
        private int _deviceSize;
        private float _deviceRadius;
        private bool _is_Built = false;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates the border of <paramref name="owner"/> with a logical width and corner radius (negative values are stored as 0).
        /// </summary>
        public YANBorder(WinControl owner, int size, int radius)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _size = Max(0, size);
            _radius = Max(0, radius);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the width of the border in 96-dpi logical pixels; a negative value is stored as 0. The owner invalidates itself.
        /// </summary>
        public int Size
        {
            get => _size;
            set
            {
                value = Max(0, value);
                if (_size != value)
                {
                    _size = value;
                    Reset();
                }
            }
        }

        /// <summary>
        /// Gets or sets the corner radius in 96-dpi logical pixels; a negative value is stored as 0. The owner invalidates itself.
        /// </summary>
        public int Radius
        {
            get => _radius;
            set
            {
                value = Max(0, value);
                if (_radius != value)
                {
                    _radius = value;
                    Reset();
                }
            }
        }

        /// <summary>
        /// Gets or sets the rectangle the shape follows, in client coordinates (device pixels); null follows the client rectangle.
        /// </summary>
        public Rectangle? Bounds
        {
            get => _bounds;
            set
            {
                if (_bounds != value)
                {
                    _bounds = value;
                    Reset();
                }
            }
        }

        /// <summary>
        /// Gets the border width in device pixels at the current size and DPI: the scaled <see cref="Size"/>, at most half the smaller
        /// side of the bounds.
        /// </summary>
        public int DeviceSize
        {
            get
            {
                Ensure();
                return _deviceSize;
            }
        }

        /// <summary>
        /// Gets the corner radius in device pixels at the current size and DPI: the scaled <see cref="Radius"/>, at most half the
        /// smaller side of the bounds.
        /// </summary>
        public float DeviceRadius
        {
            get
            {
                Ensure();
                return _deviceRadius;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the corners are rounded (a device radius of at least one pixel).
        /// </summary>
        public bool IsRounded => DeviceRadius >= 1f;

        /// <summary>
        /// Gets the outline of the control: the bounds with the rounded corners. Cached and owned by the border (do not dispose it);
        /// null when the bounds are empty.
        /// </summary>
        public GraphicsPath Shape
        {
            get
            {
                Ensure();
                return _shape;
            }
        }

        /// <summary>
        /// Gets the centre line of the border, for a pen of <see cref="DeviceSize"/> (the border then lies inside the shape): for
        /// dashed or gradient pens. Cached and owned by the border; null without a border.
        /// </summary>
        public GraphicsPath Line
        {
            get
            {
                Ensure();
                return _line;
            }
        }

        /// <summary>
        /// Gets the band of the border (the shape minus its inside, see <see cref="YANPaint.BorderRing"/>), for a solid fill.
        /// Cached and owned by the border; null without a border.
        /// </summary>
        public GraphicsPath Ring
        {
            get
            {
                Ensure();
                return _ring;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Fills the shape with a solid color, anti-aliased (nothing for a fully transparent color).
        /// </summary>
        public void FillShape(Graphics graphics, Color color)
        {
            if (color.A == 0 || Shape == null)
            {
                return;
            }
            using var brush = new SolidBrush(color);
            FillShape(graphics, brush);
        }

        /// <summary>
        /// Fills the shape with a brush (a gradient, a texture), anti-aliased.
        /// </summary>
        public void FillShape(Graphics graphics, Brush brush)
        {
            var shape = Shape;
            if (shape == null)
            {
                return;
            }
            var state = graphics.Save();
            try
            {
                SetCrisp(graphics);
                graphics.FillPath(brush, shape);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        /// <summary>
        /// Paints the border inside the shape, anti-aliased (nothing without a border or for a fully transparent color). The band is
        /// filled rather than stroked, so its straight edges are crisp.
        /// </summary>
        public void DrawBorder(Graphics graphics, Color color)
        {
            var ring = Ring;
            if (ring == null || color.A == 0)
            {
                return;
            }
            using var brush = new SolidBrush(color);
            var state = graphics.Save();
            try
            {
                SetCrisp(graphics);
                graphics.FillPath(brush, ring);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        /// <summary>
        /// Strokes the border with a pen whose width is set to <see cref="DeviceSize"/> (a dashed or gradient pen, for example; a pen
        /// the caller created, not a read-only system pen).
        /// </summary>
        public void DrawBorder(Graphics graphics, Pen pen)
        {
            var line = Line;
            if (line == null)
            {
                return;
            }
            pen.Width = _deviceSize;
            var state = graphics.Save();
            try
            {
                SetCrisp(graphics);
                graphics.DrawPath(pen, line);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        /// <summary>
        /// Fills a line of <see cref="DeviceSize"/> along the bottom of the bounds (the underlined style of the edit boxes), exactly
        /// on its rows whatever the smoothing mode of the graphics (restored afterwards).
        /// </summary>
        public void DrawUnderline(Graphics graphics, Color color)
        {
            var size = DeviceSize;
            if (size <= 0 || color.A == 0)
            {
                return;
            }
            var bounds = _builtBounds;
            using var brush = new SolidBrush(color);
            var state = graphics.Save();
            try
            {
                SetCrisp(graphics);
                graphics.FillRectangle(brush, bounds.X, bounds.Bottom - size, bounds.Width, size);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        /// <summary>
        /// Gets a value indicating whether a point (client coordinates) lies inside the shape.
        /// </summary>
        public bool Contains(Point point) => Shape?.IsVisible(point) ?? false;

        /// <summary>
        /// Lets the clicks on the transparent corners through to the parent: call it after <c>base.WndProc</c>. For a
        /// WM_NCHITTEST outside the rounded shape it sets the result to HTTRANSPARENT and returns true.
        /// </summary>
        public bool HitTest(ref Message m)
        {
            if (m.Msg != WM_NCHITTEST || !IsRounded)
            {
                return false;
            }
            // the screen coordinates are signed 16-bit values (monitors left of or above the primary one)
            var lParam = unchecked((int)m.LParam.ToInt64());
            var point = _owner.PointToClient(new Point((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF)));
            if (Contains(point))
            {
                return false;
            }
            m.Result = (IntPtr)HTTRANSPARENT;
            return true;
        }

        /// <summary>
        /// Drops the cached shape; it is rebuilt at the next use. Size and DPI changes are detected by themselves: call it from
        /// <c>RescaleConstantsForDpi</c> anyway, so that nothing built for the old DPI survives.
        /// </summary>
        public void Reset()
        {
            _shape?.Dispose();
            _line?.Dispose();
            _ring?.Dispose();
            _shape = null;
            _line = null;
            _ring = null;
            _is_Built = false;
        }

        /// <summary>
        /// Releases the cached paths.
        /// </summary>
        public void Dispose() => Reset();

        // Build the shape for the current bounds and DPI, unless it is already built for them
        private void Ensure()
        {
            var bounds = _bounds ?? _owner.ClientRectangle;
            var dpi = GetDpi(_owner);
            if (_is_Built && bounds == _builtBounds && dpi == _builtDpi)
            {
                return;
            }
            Reset();
            _builtBounds = bounds;
            _builtDpi = dpi;
            _is_Built = true;
            var isEmpty = bounds.Width <= 0 || bounds.Height <= 0;
            _deviceSize = isEmpty ? 0 : Max(0, Min(LogicalToDevice(_size, dpi), Min(bounds.Width, bounds.Height) / 2));
            _deviceRadius = isEmpty ? 0f : YANShape.EffectiveRadius(bounds, LogicalToDevice(_radius, dpi));
            _shape = YANShape.RoundedRect(bounds, _deviceRadius);
            _line = _deviceSize > 0 ? BorderPath(bounds, _deviceRadius, _deviceSize) : null;
            _ring = _deviceSize > 0 ? BorderRing(bounds, _deviceRadius, _deviceSize) : null;
        }
        #endregion
    }
}
