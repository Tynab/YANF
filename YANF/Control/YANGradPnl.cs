using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;

namespace YANF.Control
{
    /// <summary>
    /// A panel painted with a linear gradient from <see cref="TopColor"/> to <see cref="BottomColor"/>. The gradient is the
    /// panel's background, so the child controls that show what is behind them (a transparent BackColor, and the rounded corners
    /// of the YANF controls) show the gradient, not the panel's BackColor. It keeps its colors in high contrast mode.
    /// </summary>
    [ToolboxBitmap(typeof(Panel))]
    public class YANGradPnl : Panel
    {
        #region Fields
        private Color _topColor = RoyalBlue;
        private Color _bottomColor = HotPink;
        private float _angle;
        #endregion

        #region Constructors
        public YANGradPnl()
        {
            // the gradient depends on the whole client size: repaint all of it on resize, through a back buffer
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        #endregion

        #region Properties
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
        #endregion

        #region Overridden
        /// <summary>
        /// Paints the gradient as the background of the panel: its children see it too when they paint what is behind them (a
        /// transparent BackColor, and the YANF controls, which paint their parent's own pixels behind their rounded corners).
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // an opaque gradient covers the BackColor, the BackgroundImage and (for a transparent BackColor) the parent: skip them
            if (_topColor.A < 255 || _bottomColor.A < 255)
            {
                base.OnPaintBackground(e);
            }
            var rectSurface = ClientRectangle;
            // only the area to paint: a child that paints its parent asks for the part behind it
            var rectPaint = Rectangle.Intersect(rectSurface, e.ClipRectangle);
            if (rectPaint.Width > 0 && rectPaint.Height > 0)
            {
                // the gradient always spans the whole client area; flipped tiles keep GDI+ from wrapping the end color onto the
                // first row or column
                using var brush = new LinearGradientBrush(rectSurface, _topColor, _bottomColor, _angle)
                {
                    WrapMode = WrapMode.TileFlipXY
                };
                e.Graphics.FillRectangle(brush, rectPaint);
            }
        }
        #endregion
    }
}