using System.Windows.Forms;
using YANF.Screen;
using static YANF.Script.YANConstant;

namespace YANF.Script
{
    public abstract class YANMessageBox
    {
        public static DialogResult Show(string text)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(text))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string text, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(text, lang))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, lang))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, lang))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon, lang))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon, btnDflt))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon, btnDflt, lang))
            {
                res = msgFrm.ShowDialog();
            }
            return res;
        }

        /* IWin32Window Owner */

        public static DialogResult Show(IWin32Window owner, string text)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(text))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string text, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(text, lang))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, lang))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, lang))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon, lang))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon, btnDflt))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt, MsgBoxLang lang)
        {
            DialogResult res;
            using (var msgFrm = new YANMessageBoxScreen(cap, text, btns, icon, btnDflt, lang))
            {
                res = msgFrm.ShowDialog(owner);
            }
            return res;
        }
    }
}
