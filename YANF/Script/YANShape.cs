using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using static System.Math;

namespace YANF.Script
{
    /// <summary>
    /// Shared geometry and region helpers for the rounded controls and screens.
    /// </summary>
    internal static class YANShape
    {
        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgnNative(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        /// <summary>
        /// Clamps a corner radius to [0, min(width, height) / 2] of the rectangle.
        /// </summary>
        public static float EffectiveRadius(RectangleF rect, float radius) => Max(0f, Min(radius, Min(rect.Width, rect.Height) / 2f));

        /// <summary>
        /// Builds a rounded-rectangle path. Returns null for an empty rectangle and a plain rectangle when the effective radius is below one pixel,
        /// so callers never hand a zero or negative size to GraphicsPath.AddArc or to a gradient brush.
        /// </summary>
        public static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }
            var path = new GraphicsPath();
            var curveSize = EffectiveRadius(rect, radius) * 2f;
            if (curveSize < 2f)
            {
                path.AddRectangle(rect);
                return path;
            }
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Assigns a new region to the control and disposes the previous one. Passing null removes the region.
        /// Call it when the size or shape changes, never from OnPaint: assigning a region invalidates the window.
        /// </summary>
        public static void SetRegion(System.Windows.Forms.Control ctrl, Region region)
        {
            var old = ctrl.Region;
            ctrl.Region = region;
            if (old != null && !ReferenceEquals(old, region))
            {
                old.Dispose();
            }
        }

        /// <summary>
        /// Assigns a rounded region built from the path (a rectangle when the path is null) and disposes the previous region.
        /// </summary>
        public static void SetRegion(System.Windows.Forms.Control ctrl, GraphicsPath path) => SetRegion(ctrl, path == null ? null : new Region(path));

        /// <summary>
        /// Rounds the corners of a top-level window with a GDI round-rect region and frees the native handle afterwards.
        /// A corner of 0 or less removes the region.
        /// </summary>
        public static void SetRoundRegion(System.Windows.Forms.Control ctrl, int corner)
        {
            if (corner <= 0)
            {
                SetRegion(ctrl, (Region)null);
                return;
            }
            var hRgn = CreateRoundRectRgnNative(0, 0, ctrl.Width, ctrl.Height, corner, corner);
            if (hRgn == IntPtr.Zero)
            {
                return;
            }
            try
            {
                // Region.FromHrgn copies the region, so the GDI handle can be released right away
                SetRegion(ctrl, Region.FromHrgn(hRgn));
            }
            finally
            {
                _ = DeleteObject(hRgn);
            }
        }
    }
}
