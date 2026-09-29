using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;

namespace YANF.Control
{
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

        [Category("YAN Appearance"), Description("Angle of the gradient.")]
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
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            var rectSurface = ClientRectangle;
            if (rectSurface.Width > 0 && rectSurface.Height > 0)
            {
                using var brush = new LinearGradientBrush(rectSurface, _topColor, _bottomColor, _angle);
                e.Graphics.FillRectangle(brush, rectSurface);
            }
        }
        #endregion
    }
}