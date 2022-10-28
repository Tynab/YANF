using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YANF.Script
{
    public static class YANEvent
    {
        #region Form Move
        private static bool _isMove;
        private static Point _lastLoc;

        /// <summary>
        /// Focus control dùng để di chuyển form.
        /// </summary>
        public static void MoveFrm_MouseDown(object sender, MouseEventArgs e)
        {
            _isMove = true;
            _lastLoc = e.Location;
            ((System.Windows.Forms.Control)sender).FindForm().Opacity = 0.7;
        }

        /// <summary>
        /// Di chuyển control.
        /// </summary>
        public static void MoveFrm_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMove)
            {
                var frm = ((System.Windows.Forms.Control)sender).FindForm();
                frm.Location = new Point(frm.Location.X - _lastLoc.X + e.X, frm.Location.Y - _lastLoc.Y + e.Y);
                frm.Update();
            }
        }

        /// <summary>
        /// Kết thúc di chyển.
        /// </summary>
        public static void MoveFrm_MouseUp(object sender, MouseEventArgs e)
        {
            _isMove = false;
            ((System.Windows.Forms.Control)sender).FindForm().Opacity = 1;
        }
        #endregion
    }
}
