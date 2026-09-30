using System;
using System.Windows.Forms;
using YANF.Screen;
using static YANF.Script.YANConstant;

namespace YANF.Script
{
    public abstract class YANMessageBox
    {
        /// <summary>
        /// Shows the message box that <paramref name="options"/> describes, with no explicit owner: WinForms makes the active window of
        /// the calling thread, if there is one, its owner (as the overloads without an owner always did). Call it from an STA UI thread.
        /// </summary>
        /// <param name="options">What the box shows.</param>
        /// <returns>The result of the button that closed the box.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException"><see cref="YANMessageBoxOptions.Buttons"/> is not a <see cref="MessageBoxButtons"/> value.</exception>
        public static DialogResult Show(YANMessageBoxOptions options) => Show(null, options);

        /// <summary>
        /// Shows the message box that <paramref name="options"/> describes, modal to <paramref name="owner"/>. When <paramref name="owner"/>
        /// is a <see cref="System.Windows.Forms.Control"/> whose window belongs to another UI thread, the box is shown on that thread (which
        /// must be pumping messages, not waiting for this call) and this call blocks until the box closes. Without such an owner, call it
        /// from an STA UI thread.
        /// </summary>
        /// <param name="owner">The window that owns the box, or null.</param>
        /// <param name="options">What the box shows.</param>
        /// <returns>The result of the button that closed the box.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="System.ComponentModel.InvalidEnumArgumentException"><see cref="YANMessageBoxOptions.Buttons"/> is not a <see cref="MessageBoxButtons"/> value.</exception>
        public static DialogResult Show(IWin32Window owner, YANMessageBoxOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            // the box belongs on the owner's UI thread
            if (owner is System.Windows.Forms.Control ctrl && ctrl.InvokeRequired)
            {
                return (DialogResult)ctrl.Invoke(new Func<DialogResult>(() => Show(owner, options)));
            }
            using var msgFrm = new YANMessageBoxScreen(options);
            return msgFrm.ShowDialog(owner);
        }

        public static DialogResult Show(string text) => Show(null, new YANMessageBoxOptions { Text = text });

        public static DialogResult Show(string text, MsgBoxLang lang) => Show(null, new YANMessageBoxOptions { Text = text, Language = lang });

        public static DialogResult Show(string cap, string text) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text });

        public static DialogResult Show(string cap, string text, MsgBoxLang lang) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text, Language = lang });

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns });

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MsgBoxLang lang) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Language = lang });

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon });

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MsgBoxLang lang) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon, Language = lang });

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon, DefaultButton = btnDflt });

        public static DialogResult Show(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt, MsgBoxLang lang) => Show(null, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon, DefaultButton = btnDflt, Language = lang });

        /* IWin32Window Owner */

        public static DialogResult Show(IWin32Window owner, string text) => Show(owner, new YANMessageBoxOptions { Text = text });

        public static DialogResult Show(IWin32Window owner, string text, MsgBoxLang lang) => Show(owner, new YANMessageBoxOptions { Text = text, Language = lang });

        public static DialogResult Show(IWin32Window owner, string cap, string text) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text });

        public static DialogResult Show(IWin32Window owner, string cap, string text, MsgBoxLang lang) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text, Language = lang });

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns });

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MsgBoxLang lang) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Language = lang });

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon });

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MsgBoxLang lang) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon, Language = lang });

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon, DefaultButton = btnDflt });

        public static DialogResult Show(IWin32Window owner, string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt, MsgBoxLang lang) => Show(owner, new YANMessageBoxOptions { Caption = cap, Text = text, Buttons = btns, Icon = icon, DefaultButton = btnDflt, Language = lang });
    }
}