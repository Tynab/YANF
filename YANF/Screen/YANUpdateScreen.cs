using System;
using static System.Windows.Forms.DialogResult;
using static YANF.Script.YANDisplay;

namespace YANF.Screen
{
    public partial class YANUpdateScreen : MiddleScreen
    {
        #region Constructors
        public YANUpdateScreen() => InitializeComponent();
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
        // Shown form
        private void YANUpdateScreen_Shown(object sender, EventArgs e) => this.FadeIn();
        #endregion
    }
}