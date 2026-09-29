using System;
using System.Drawing;
using static System.Math;
using static System.Windows.Forms.DialogResult;
using static System.Windows.Forms.FormStartPosition;
using static YANF.Script.YANDisplay;

namespace YANF.Screen
{
    public partial class YANUpdateScreen : MiddleScreen
    {
        #region Constructors
        public YANUpdateScreen() => InitializeComponent();

        // Centre on a snapshot of the caller's bounds, kept inside the working area of that display (what CenterParent does with an owner)
        internal YANUpdateScreen(Rectangle parentBounds) : this()
        {
            var area = System.Windows.Forms.Screen.FromRectangle(parentBounds).WorkingArea;
            var x = (parentBounds.Left + parentBounds.Right - Width) / 2;
            var y = (parentBounds.Top + parentBounds.Bottom - Height) / 2;
            StartPosition = Manual;
            Location = new Point(Max(area.Left, Min(x, area.Right - Width)), Max(area.Top, Min(y, area.Bottom - Height)));
        }
        #endregion

        #region Overridden
        public override void Frm_Close()
        {
            if (IsDisposed)
            {
                return;
            }
            DialogResult = OK;
            this.FadeOut();
            Dispose();
        }
        #endregion

        #region Events
        // Shown frm
        private void YANUpdateScreen_Shown(object sender, EventArgs e) => this.FadeIn();
        #endregion
    }
}