using System.Windows.Forms;

namespace YANF.Screen
{
    public abstract class YANAnonScreen : YANHardScreen
    {
        #region Overridden
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Form close event.
        /// </summary>
        public abstract void Frm_Close();
        #endregion
    }
}