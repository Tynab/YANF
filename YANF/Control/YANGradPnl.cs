﻿using System.ComponentModel;
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

        #region Properties
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
        #endregion

        #region Overridden
        protected override void OnPaint(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(ClientRectangle, _topColor, _bottomColor, _angle))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            base.OnPaint(e);
        }
        #endregion
    }
}