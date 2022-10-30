using System;
using System.Drawing;
using System.Windows.Forms;
using YANF.Script;
using static System.Drawing.Region;
using static System.Windows.Forms.DialogResult;
using static System.Windows.Forms.FormStartPosition;
using static YANF.Script.YANDisplay;

namespace YANF.Screen
{
    public partial class YANLoadScreen : MiddleScreen
    {
        #region Constructors
        public YANLoadScreen(Form pFrm, int corner, bool isTop)
        {
            InitializeComponent();
            StartPosition = Manual;
            Location = new Point(pFrm.Location.X, pFrm.Location.Y);
            Width = pFrm.Width;
            Height = pFrm.Height;
            TopMost = isTop;
            Region = FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, corner, corner));
        }
        #endregion

        #region Overridden
        public override void Frm_Close()
        {
            DialogResult = OK;
            this.FadeOut();
            Dispose();
        }
        #endregion

        #region Events
        // Shown frm
        private void YANLoadScreen_Shown(object sender, EventArgs e) => this.FadeIn();
        #endregion
    }
}
