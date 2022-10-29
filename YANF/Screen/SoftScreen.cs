using System.Windows.Forms;

namespace YANF.Screen
{
    public class SoftScreen : HardScreen
    {
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
    }
}
