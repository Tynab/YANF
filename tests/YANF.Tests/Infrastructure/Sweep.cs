using System.Drawing;

namespace YANF.Tests
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// Shared size and shape sweeps: zero, tiny and normal sizes, and radius/border pairs where the border is larger than the
    /// radius, both exceed half the size, or are negative (negative values must be stored as 0, big ones clamped at paint time).
    /// </summary>
    internal static class Sweep
    {
        #region Fields
        /// <summary>
        /// Sizes for the custom-painted controls.
        /// </summary>
        public static readonly Size[] PaintSizes = { new(0, 0), new(1, 1), new(10, 3), new(200, 40), new(100, 100) };

        /// <summary>
        /// (radius, border) pairs for the custom-painted controls.
        /// </summary>
        public static readonly (int Radius, int Border)[] PaintShapes = { (0, 0), (2, 2), (20, 25), (25, 20), (49, 49), (49, 0), (0, 49), (50, 50), (1000, 1000), (-5, -3), (-1, 5), (5, -1) };

        /// <summary>
        /// Sizes for the composite controls (YANTxt, YANNb).
        /// </summary>
        public static readonly Size[] CompositeSizes = { new(0, 0), new(1, 1), new(10, 3), new(40, 40), new(200, 30), new(200, 80) };

        /// <summary>
        /// (radius, border) pairs for the composite controls; radius 16 is the first that also rounds the inner control.
        /// </summary>
        public static readonly (int Radius, int Border)[] CompositeShapes = { (0, 0), (2, 2), (16, 2), (20, 25), (25, 20), (15, 15), (49, 49), (0, 49), (1000, 1000), (-5, -3), (-1, 5), (5, -1) };
        #endregion

        #region Methods
        /// <summary>
        /// Sizes a control past its MinimumSize, so the tiny sizes are really exercised.
        /// </summary>
        public static T Sized<T>(T c, Size size) where T : Control
        {
            c.MinimumSize = Size.Empty;
            c.Size = size;
            return c;
        }
        #endregion
    }
}
