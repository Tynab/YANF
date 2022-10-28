using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Drawing.FontStyle;
using static System.Runtime.InteropServices.CharSet;
using static System.Threading.Thread;
using static YANF.Script.YANConstant;

namespace YANF.Script
{
    public static class YANDisplay
    {
        /// <summary>
        /// Tạo khung ellipse cho form.
        /// </summary>
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        /// <summary>
        /// Điều khiển object với animation đồng bộ.
        /// </summary>
        [DllImport("user32.dll", CharSet = Auto)]
        public static extern void AnimateWindow(IntPtr hWnd, int time, AnimateWindowFlags flags);

        /// <summary>
        /// Fade in form.
        /// </summary>
        public static void FadeIn(this Form frm)
        {
            while (frm.Opacity < 1)
            {
                frm.Opacity += 0.05;
                frm.Update();
                Sleep(10);
            }
        }

        /// <summary>
        /// Fade out form.
        /// </summary>
        public static void FadeOut(this Form frm)
        {
            while (frm.Opacity > 0)
            {
                frm.Opacity -= 0.05;
                frm.Update();
                Sleep(10);
            }
        }

        /// <summary>
        /// Highlight label link bằng tên control (prefix tên label bắt buộc là "lbl").
        /// </summary>
        /// <param name="typeName">Loại control.</param>
        /// <param name="color">Màu highlight.</param>
        /// <param name="isBold">In đậm hoặc không.</param>
        public static void HighLightLblLinkByCtrl(this System.Windows.Forms.Control ctrl, string typeName, Color color, bool isBold)
        {
            var lbl = (Label)ctrl.FindForm().Controls.Find($"lbl{ctrl.Name.Substring(typeName.Length)}", true).FirstOrDefault();
            if (lbl != null)
            {
                lbl.ForeColor = color;
                lbl.Font = isBold ? new Font(lbl.Font, Bold) : new Font(lbl.Font, Regular);
            }
        }
    }
}