using System.Drawing;
using System.Windows.Forms;

namespace YANF.Screen
{
    public partial class YANLoadScreen : YANOverlayScreen
    {
        #region Constructors
        public YANLoadScreen(Form pFrm, int corner, bool isTop) : this(pFrm.Bounds, corner, isTop)
        {
        }

        // Bounds are passed as a snapshot so the screen can be built on its own thread without touching pFrm
        internal YANLoadScreen(Rectangle bounds, int corner, bool isTop) : this() => PlaceAt(bounds, corner, isTop);

        // YANLoader: placed over its owner when shown
        internal YANLoadScreen() => InitializeComponent();
        #endregion

        #region Overridden
        /// <summary>
        /// Fades the screen out, then closes and disposes it (see <see cref="YANOverlayScreen.Frm_Close"/>).
        /// </summary>
        public override void Frm_Close() => base.Frm_Close();

        /// <summary>
        /// Shows the percentage; the detail text is not shown on this screen.
        /// </summary>
        protected internal override void SetProgress(int percent, string detail) => lblPercent.Text = $"{percent}%";
        #endregion
    }
}
