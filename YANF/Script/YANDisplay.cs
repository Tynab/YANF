using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
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
    }
}