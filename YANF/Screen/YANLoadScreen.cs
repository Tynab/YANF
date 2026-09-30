using System.Drawing;
using YANF.Script;

namespace YANF.Screen
{
    /// <summary>
    /// Loading animation with a percentage, covering the window it is shown over (<see cref="YANLoader"/> and the legacy YANLoadScrService).
    /// </summary>
    internal partial class YANLoadScreen : YANOverlayScreen
    {
        #region Constructors
        // Legacy service: fixed placement from a snapshot of the caller's bounds (the screen is built on its own thread)
        internal YANLoadScreen(Rectangle bounds, int corner, bool isTop) : this() => PlaceAt(bounds, corner, isTop);

        // YANLoader: placed over its owner when shown
        internal YANLoadScreen()
        {
            InitializeComponent();
            this.ScaleToDpi();
        }
        #endregion

        #region Overridden
        /// <summary>
        /// Shows the percentage; the detail text is not shown on this screen.
        /// </summary>
        protected internal override void SetProgress(int percent, string detail) => lblPercent.Text = $"{percent}%";
        #endregion
    }
}
