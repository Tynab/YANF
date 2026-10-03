using System.Drawing;
using YANF.Script;
using static System.Math;

namespace YANF.Screen
{
    /// <summary>
    /// Small update box with a percentage, a detail text and a progress bar, centred on the window it is shown over
    /// (<see cref="YANLoader"/> and the legacy YANUpdScrService).
    /// </summary>
    internal partial class YANUpdateScreen : YANOverlayScreen
    {
        #region Constructors
        // YANLoader (placed over its owner when shown) and the legacy service without a usable parent (centred on the screen)
        internal YANUpdateScreen()
        {
            InitializeComponent();
            this.ScaleToDpi();
        }

        // Centre on a snapshot of the caller's bounds (see ComputeBounds)
        internal YANUpdateScreen(Rectangle parentBounds) : this() => PlaceAt(parentBounds);
        #endregion

        #region Overridden
        /// <summary>
        /// Shows the percentage, the detail text (for example the downloaded size) and a progress bar sized from the screen's own width.
        /// </summary>
        protected internal override void SetProgress(int percent, string detail) => ShowBar(percent, detail, (int)Ceiling(panelMain.ClientSize.Width * Max(0, Min(100, percent)) / 100d));

        /// <summary>
        /// Centres the screen on the window, kept inside the working area of that window's display (what CenterParent does with an owner).
        /// </summary>
        protected override Rectangle ComputeBounds(Rectangle ownerBounds)
        {
            var area = System.Windows.Forms.Screen.FromRectangle(ownerBounds).WorkingArea;
            var x = (ownerBounds.Left + ownerBounds.Right - Width) / 2;
            var y = (ownerBounds.Top + ownerBounds.Bottom - Height) / 2;
            return new Rectangle(Max(area.Left, Min(x, area.Right - Width)), Max(area.Top, Min(y, area.Bottom - Height)), Width, Height);
        }
        #endregion

        #region Methods
        // Legacy PublishValue: the caller's bar width is in 96-dpi pixels (out of YANConstant.W_UPDATE_SCR, the width of the screen at
        // 96 dpi), so it is scaled like the screen itself; the same pixels as 1.0 at 96 dpi
        internal void ShowValues(int percent, string capacity, int width) => ShowBar(percent, capacity, this.LogicalToDevice(width));

        // Show the three values; the bar width is in device pixels
        private void ShowBar(int percent, string detail, int barWidth)
        {
            lblPercent.Text = $"{percent}%";
            pnlProgressBar.Width = barWidth;
            lblCapacity.Text = detail;
        }
        #endregion
    }
}
