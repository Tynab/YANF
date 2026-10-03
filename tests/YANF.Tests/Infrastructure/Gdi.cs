using System;
using System.Drawing;
using System.Reflection;

namespace YANF.Tests
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// GDI+ and pixel helpers.
    /// </summary>
    internal static class Gdi
    {
        #region Methods
        /// <summary>
        /// True when the font has been disposed (its native GDI+ handle is gone).
        /// </summary>
        public static bool IsDisposed(Font font)
        {
            try
            {
                _ = font.GetHeight();
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            // Some GDI+ implementations answer from cached data: check the native handle as well
            var handle = typeof(Font).GetField("nativeFont", BindingFlags.NonPublic | BindingFlags.Instance) ?? typeof(Font).GetField("fontObject", BindingFlags.NonPublic | BindingFlags.Instance);
            return handle != null && (IntPtr)handle.GetValue(font) == IntPtr.Zero;
        }

        /// <summary>
        /// True when the image has been disposed.
        /// </summary>
        public static bool IsDisposed(Image image)
        {
            try
            {
                _ = image.Width;
                using (new Bitmap(image))
                {
                }
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
            catch (NullReferenceException)
            {
                return true;
            }
        }

        /// <summary>
        /// True when the region has been disposed.
        /// </summary>
        public static bool IsDisposed(Region region)
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);
            try
            {
                _ = region.GetBounds(g);
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        /// <summary>
        /// Exact ARGB comparison.
        /// </summary>
        public static bool Same(Color a, Color b) => a.ToArgb() == b.ToArgb();

        /// <summary>
        /// Strongly red (text or fill in pure red, anti-aliased edges excluded).
        /// </summary>
        public static bool IsRed(Color c) => c.R > 200 && c.G < 80 && c.B < 80;

        /// <summary>
        /// Strongly green (pure lime, anti-aliased edges excluded).
        /// </summary>
        public static bool IsLime(Color c) => c.G > 200 && c.R < 80 && c.B < 80;

        /// <summary>
        /// Dark gray (text or password dots in a gray fore color such as DimGray).
        /// </summary>
        public static bool IsDarkGray(Color c) => c.R < 150 && c.G < 150 && c.B < 150 && Math.Abs(c.R - c.G) < 30 && Math.Abs(c.G - c.B) < 30;

        /// <summary>
        /// Counts the pixels of the bitmap (optionally from column <paramref name="fromX"/>) that match <paramref name="match"/>.
        /// </summary>
        public static int Count(Bitmap bmp, Func<Color, bool> match, int fromX = 0)
        {
            var n = 0;
            for (var x = fromX; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    if (match(bmp.GetPixel(x, y)))
                    {
                        n++;
                    }
                }
            }
            return n;
        }

        /// <summary>
        /// Counts the matching pixels of the control as it is shown on screen. Needed where a control paints outside WM_PAINT's
        /// device context (the YANTxt placeholder is drawn with GetDC after WM_PAINT, which DrawToBitmap cannot see).
        /// </summary>
        public static int CountOnScreen(Control c, Func<Color, bool> match)
        {
            using var bmp = new Bitmap(c.Width, c.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(c.PointToScreen(Point.Empty), Point.Empty, c.Size);
            }
            return Count(bmp, match);
        }
        #endregion
    }
}
