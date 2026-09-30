using System.ComponentModel;
using System.Windows.Forms;
using static System.Windows.Forms.Keys;

namespace YANF.Screen
{
    public class HardScreen : Form
    {
        #region Properties
        /// <summary>
        /// When true (the default, as in 1.0), Alt+F4 is swallowed and does not close the form; when false, Alt+F4 closes it as usual.
        /// </summary>
        [Category("YAN Behavior"), Description("When this property is true, Alt+F4 does not close the form.")]
        [DefaultValue(true)]
        public bool BlockAltF4 { get; set; } = true;
        #endregion

        #region Overridden
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) => (BlockAltF4 && keyData == (Alt | F4)) || base.ProcessCmdKey(ref msg, keyData);
        #endregion
    }
}
