using System;
using System.Drawing;
using System.Windows.Forms;
using YANF.Script;
using static System.Windows.Forms.DialogResult;
using static System.Windows.Forms.FormStartPosition;
using static YANF.Script.YANDisplay;
using static YANF.Script.YANShape;

namespace YANF.Screen
{
    public partial class YANLoadScreen : MiddleScreen
    {
        #region Constructors
        public YANLoadScreen(Form pFrm, int corner, bool isTop) : this(pFrm.Bounds, corner, isTop)
        {
        }

        // Bounds are passed as a snapshot so the screen can be built on its own thread without touching pFrm
        internal YANLoadScreen(Rectangle bounds, int corner, bool isTop)
        {
            InitializeComponent();
            StartPosition = Manual;
            Location = bounds.Location;
            Width = bounds.Width;
            Height = bounds.Height;
            TopMost = isTop;
            SetRoundRegion(this, corner);
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
        private void YANLoadScreen_Shown(object sender, EventArgs e) => this.FadeIn();
        #endregion
    }
}
