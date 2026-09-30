using System.Drawing;
using static System.Math;

namespace YANF.Screen
{
    public partial class YANUpdateScreen : YANOverlayScreen
    {
        #region Constructors
        public YANUpdateScreen() => InitializeComponent();

        // Centre on a snapshot of the caller's bounds (see ComputeBounds)
        internal YANUpdateScreen(Rectangle parentBounds) : this() => PlaceAt(parentBounds);
        #endregion

        #region Overridden
        /// <summary>
        /// Fades the screen out, then closes and disposes it (see <see cref="YANOverlayScreen.Frm_Close"/>).
        /// </summary>
        public override void Frm_Close() => base.Frm_Close();

        /// <summary>
        /// Shows the percentage, the detail text (for example the downloaded size) and a progress bar sized from the screen's own width.
        /// </summary>
        protected internal override void SetProgress(int percent, string detail) => ShowValues(percent, detail, (int)Ceiling(panelMain.ClientSize.Width * Max(0, Min(100, percent)) / 100d));

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
        // Show the three values; the bar width is in pixels (the legacy PublishValue passes it as is)
        internal void ShowValues(int percent, string capacity, int width)
        {
            lblPercent.Text = $"{percent}%";
            pnlProgressBar.Width = width;
            lblCapacity.Text = capacity;
        }
        #endregion
    }
}