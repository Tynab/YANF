using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Math;
using static YANF.Control.YANPaint;
using static YANF.Script.YANShape;

namespace YANF.Control
{
    using WinControl = System.Windows.Forms.Control;

    /// <summary>
    /// Painting shared by the edit boxes (<see cref="YANTxt"/>, <see cref="YANNb"/>): a surface with anti-aliased rounded corners over
    /// the painted parent, an inner editor (a TextBox, a NumericUpDown) that stays a rectangular child window clipped by a region built
    /// when the size, the shape or the DPI changes (never while painting), and the border or the underline in the 1.x geometry.
    /// </summary>
    internal static class YANEditPaint
    {
        #region Fields
        /// <summary>
        /// Above this radius (96-dpi pixels) the inner editor of an edit box gets rounded corners of its own, as in 1.x.
        /// </summary>
        public const int EDIT_REGION_RADIUS = 15;
        #endregion

        #region Methods
        /// <summary>
        /// Updates the region of the inner editor of an edit box, never while painting: the box itself has no region (its corners are
        /// painted over the parent), so the editor is clipped where the rounded corners would show it, as the 1.x region of the box
        /// clipped it; above a radius of <see cref="EDIT_REGION_RADIUS"/> its own corners are also rounded as in 1.x (following the
        /// radius of the box when <paramref name="isFollowingRadius"/>, for a multiline text box, otherwise twice the border). An editor
        /// inside the rounded shape gets no region.
        /// </summary>
        public static void UpdateEditRegion(WinControl ctrl, WinControl inner, YANBorder border, bool isFollowingRadius)
        {
            Region region = null;
            var radius = (int)border.DeviceRadius;
            if (radius > LogicalToDevice(ctrl, EDIT_REGION_RADIUS))
            {
                var size = border.DeviceSize;
                using var pathInner = RoundedRect(inner.ClientRectangle, isFollowingRadius ? radius - size : size * 2);
                region = pathInner == null ? null : new Region(pathInner);
            }
            var shape = border.Shape;
            var bounds = inner.Bounds;
            if (border.IsRounded && shape != null && bounds.Width > 0 && bounds.Height > 0 && !IsInShape(shape, bounds))
            {
                // the shape of the box in the coordinates of the editor
                using var pathShape = (GraphicsPath)shape.Clone();
                using var matrix = new Matrix();
                matrix.Translate(-bounds.X, -bounds.Y);
                pathShape.Transform(matrix);
                if (region == null)
                {
                    region = new Region(pathShape);
                }
                else
                {
                    region.Intersect(pathShape);
                }
            }
            SetRegion(inner, region);
        }

        /// <summary>
        /// Paints the background of an edit box: an opaque square box gets the default background (BackColor and BackgroundImage, as
        /// in 1.x) and hides what is behind it; otherwise the parent's own pixels are painted first, then the surface anti-aliased in
        /// the rounded shape (the BackgroundImage clipped to it).
        /// </summary>
        public static void PaintEditSurface(WinControl ctrl, PaintEventArgs e, YANBorder border, Action<PaintEventArgs> paintDefaultBackground)
        {
            var backColor = ctrl.BackColor;
            if (!border.IsRounded && backColor.A == 255)
            {
                paintDefaultBackground(e);
                return;
            }
            PaintParent(ctrl, e);
            border.FillShape(e.Graphics, backColor);
            var shape = border.Shape;
            if (ctrl.BackgroundImage != null && shape != null)
            {
                var graphics = e.Graphics;
                var state = graphics.Save();
                try
                {
                    graphics.SetClip(shape, CombineMode.Intersect);
                    paintDefaultBackground(e);
                }
                finally
                {
                    graphics.Restore(state);
                }
            }
        }

        /// <summary>
        /// Paints the border of an edit box inside its shape, or the underline cut by the rounded corners: as 1.x drew it, the part
        /// inside the box of a BorderSize-wide line centred on the bottom pixel row, BorderSize / 2 + 1 rows at 96 dpi (one for 0,
        /// except on a rounded box when <paramref name="isRoundedHairline"/> is false: the rounded YANNb of 1.x drew no underline for
        /// a BorderSize of 0). System colors in high contrast mode.
        /// </summary>
        public static void PaintEditBorder(WinControl ctrl, Graphics graphics, YANBorder border, bool isUnderlined, Color color, bool isFocus, bool isRoundedHairline)
        {
            color = Contrast(color, isFocus ? SystemColors.Highlight : SystemColors.WindowFrame);
            if (!isUnderlined)
            {
                border.DrawBorder(graphics, color);
                return;
            }
            var shape = border.Shape;
            if (shape == null || color.A == 0 || (!isRoundedHairline && border.DeviceSize == 0 && (int)border.DeviceRadius > 1))
            {
                return;
            }
            var size = (int)Floor((border.DeviceSize + LogicalToDevice(ctrl, 1f)) / 2f + 0.5f);
            var client = ctrl.ClientSize;
            var state = graphics.Save();
            try
            {
                // the rows lie on whole pixels, so the clip is exact; the shape keeps the corners anti-aliased
                graphics.IntersectClip(new Rectangle(0, client.Height - size, client.Width, size));
                FillPath(graphics, color, shape);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        // Check whether a rectangle lies in the shape: its four outer corners do (a rounded rectangle is convex)
        private static bool IsInShape(GraphicsPath shape, Rectangle rect)
            => shape.IsVisible(rect.Left, rect.Top) && shape.IsVisible(rect.Right, rect.Top) && shape.IsVisible(rect.Left, rect.Bottom) && shape.IsVisible(rect.Right, rect.Bottom);
        #endregion
    }
}
