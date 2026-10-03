using System;
using System.Windows.Forms;

namespace YANF.Script
{
    public static class YANConstant
    {
        /// <summary>
        /// Width of the Update screen at 96 dpi: the full progress bar width that <c>YANUpdScrService.PublishValue</c> takes
        /// (the width is in 96-dpi pixels and is scaled with the screen at other DPIs).
        /// </summary>
        public const int W_UPDATE_SCR = 360;

        [Flags]
        public enum AnimateWindowFlags
        {
            AW_HOR_POSITIVE = 0x00000001,
            AW_HOR_NEGATIVE = 0x00000002,
            AW_VER_POSITIVE = 0x00000004,
            AW_VER_NEGATIVE = 0x00000008,
            AW_CENTER = 0x00000010,
            AW_HIDE = 0x00010000,
            AW_ACTIVATE = 0x00020000,
            AW_SLIDE = 0x00040000,
            AW_BLEND = 0x00080000
        }

        public enum PrgTextPosition
        {
            Left,
            Right,
            Center,
            Sliding,
            None
        }

        public enum MsgBoxLang
        {
            VIE,
            JAP,
            /// <summary>
            /// English: the texts and fonts that a message box without a language has always used.
            /// </summary>
            ENG
        }

        // Button text of the message box in a language: the one table of message box texts. VIE and JAP keep the 1.0
        // wording (VIE names the OK button of an OK-only box "Đóng" and "Xong" elsewhere); any other language is English
        internal static string GetMsgBoxBtnText(MsgBoxLang lang, MessageBoxButtons btns, DialogResult res) => lang switch
        {
            MsgBoxLang.VIE => res switch
            {
                DialogResult.OK => btns == MessageBoxButtons.OK ? "Đóng" : "Xong",
                DialogResult.Cancel => "Hủy",
                DialogResult.Yes => "Vâng",
                DialogResult.No => "Không",
                DialogResult.Retry => "Thử lại",
                DialogResult.Abort => "Hủy Bỏ",
                DialogResult.Ignore => "Bỏ qua",
                _ => GetMsgBoxBtnText(MsgBoxLang.ENG, btns, res)
            },
            MsgBoxLang.JAP => res switch
            {
                DialogResult.OK => "オーケー",
                DialogResult.Cancel => "キャンセル",
                DialogResult.Yes => "はい",
                DialogResult.No => "いいえ",
                DialogResult.Retry => "リトライ",
                DialogResult.Abort => "アボート",
                DialogResult.Ignore => "無視",
                _ => GetMsgBoxBtnText(MsgBoxLang.ENG, btns, res)
            },
            _ => res switch
            {
                DialogResult.OK => "OK",
                DialogResult.Cancel => "Cancel",
                DialogResult.Yes => "Yes",
                DialogResult.No => "No",
                DialogResult.Retry => "Retry",
                DialogResult.Abort => "Abort",
                DialogResult.Ignore => "Ignore",
                _ => res.ToString()
            }
        };
    }
}