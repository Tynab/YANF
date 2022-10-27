using System.Windows.Forms;
using static System.Windows.Forms.Keys;

namespace YANF.Screen
{
    public class YANHardScreen : Form
    {
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) => keyData == (Alt | F4) || base.ProcessCmdKey(ref msg, keyData);
    }
}