using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
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
        #region Fields
        // Fonts created by HighLightLblLinkByCtrl, per label (only these may be disposed when replaced)
        private static readonly ConditionalWeakTable<Label, Font> _highLightFonts = new();
        #endregion

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
        /// <param name="ctrl">Control liên kết với label.</param>
        /// <param name="typeName">Loại control.</param>
        /// <param name="color">Màu highlight.</param>
        /// <param name="isBold">In đậm hoặc không.</param>
        /// <remarks>
        /// Does nothing when the control is not on a form, its name is shorter than <paramref name="typeName"/>, or no <see cref="Label"/> with that name exists.
        /// The label font is replaced only when its Bold state changes, keeping its other style bits (italic, underline, strikeout).
        /// </remarks>
        public static void HighLightLblLinkByCtrl(this System.Windows.Forms.Control ctrl, string typeName, Color color, bool isBold)
        {
            if (ctrl is not { Name: { } name } || typeName is null || name.Length < typeName.Length || ctrl.FindForm() is not { } frm)
            {
                return;
            }
            if (frm.Controls.Find($"lbl{name.Substring(typeName.Length)}", true).OfType<Label>().FirstOrDefault() is not { } lbl)
            {
                return;
            }
            lbl.ForeColor = color;
            SetBold(lbl, isBold);
        }

        // Toggle only the Bold bit of the label font, creating a Font only when it actually changes
        private static void SetBold(Label lbl, bool isBold)
        {
            var font = lbl.Font;
            if (font.Bold == isBold)
            {
                return;
            }
            var newFont = new Font(font, isBold ? font.Style | Bold : font.Style & ~Bold);
            lbl.Font = newFont;
            // dispose the replaced font only if this helper created it for this label (never a consumer's or an inherited font)
            if (_highLightFonts.TryGetValue(lbl, out var created) && ReferenceEquals(created, font) && ReferenceEquals(lbl.Font, newFont))
            {
                font.Dispose();
            }
            _highLightFonts.Remove(lbl);
            _highLightFonts.Add(lbl, newFont);
        }
    }
}