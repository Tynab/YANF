using System.Drawing;
using YANF.Script;

namespace YANF.Screen
{
    /// <summary>
    /// Waiting animation without progress, covering the window it is shown over (<see cref="YANLoader"/> and the legacy YANWaitScrService).
    /// </summary>
    internal partial class YANWaitScreen : YANOverlayScreen
    {
        #region Constructors
        // Legacy service: fixed placement from a snapshot of the caller's bounds (the screen is built on its own thread)
        internal YANWaitScreen(Rectangle bounds, int corner, bool isTop) : this() => PlaceAt(bounds, corner, isTop);

        // YANLoader: placed over its owner when shown
        internal YANWaitScreen()
        {
            InitializeComponent();
            this.ScaleToDpi();
        }
        #endregion
    }
}
