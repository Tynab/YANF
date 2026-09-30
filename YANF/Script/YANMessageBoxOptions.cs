using System.Windows.Forms;
using static YANF.Script.YANConstant;

namespace YANF.Script
{
    /// <summary>
    /// Everything a <see cref="YANMessageBox"/> shows, for <see cref="YANMessageBox.Show(System.Windows.Forms.IWin32Window, YANMessageBoxOptions)"/>.
    /// The values are read when the message box is created. The defaults give the same box as <see cref="YANMessageBox.Show(string)"/>.
    /// </summary>
    public sealed class YANMessageBoxOptions
    {
        #region Properties
        /// <summary>
        /// Text of the title bar. Default: null (no caption).
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        /// The message. Long lines wrap at about half the width of the screen's working area.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The buttons to show. Default: <see cref="MessageBoxButtons.OK"/>.
        /// </summary>
        public MessageBoxButtons Buttons { get; set; } = MessageBoxButtons.OK;

        /// <summary>
        /// The icon, which also sets the accent color of the box. Default: <see cref="MessageBoxIcon.None"/>.
        /// </summary>
        public MessageBoxIcon Icon { get; set; } = MessageBoxIcon.None;

        /// <summary>
        /// The button that Enter presses and that has the focus when the box opens; it is marked with a border.
        /// A button that the box does not show falls back to the first button. Default: <see cref="MessageBoxDefaultButton.Button1"/>.
        /// </summary>
        public MessageBoxDefaultButton DefaultButton { get; set; } = MessageBoxDefaultButton.Button1;

        /// <summary>
        /// Language of the button texts and fonts. Default: null, the English texts and fonts that the overloads without a language use.
        /// </summary>
        public MsgBoxLang? Language { get; set; }

        /// <summary>
        /// Whether the box stays above other windows. Default: true, as every message box of 1.0 did.
        /// </summary>
        public bool TopMost { get; set; } = true;

        /// <summary>
        /// Whether the close box (✕) follows the rule of the Windows message box: it gives the result of the button that Esc presses
        /// (Cancel, or OK in an OK-only box) and is hidden when there is no such button (Yes/No and Abort/Retry/Ignore).
        /// Default: false, the close box always gives <see cref="DialogResult.Cancel"/>, as in 1.0.
        /// </summary>
        public bool StrictClose { get; set; }
        #endregion
    }
}
