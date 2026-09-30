using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.ComponentModel.EditorBrowsableState;
using static System.Math;
using static System.Windows.Forms.MouseButtons;

namespace YANF.Script
{
    public static class YANEvent
    {
        #region Form Drag
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 2;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Lets the user move the form by dragging this control with the left mouse button, like a title bar.
        /// </summary>
        /// <param name="handle">Control used as a drag handle (a header panel, a caption label, a picture), or the form itself.</param>
        /// <remarks>
        /// The drag is handed to Windows' own window-move loop (as if the title bar were pressed), so Aero Snap, multi-monitor moves,
        /// Esc to cancel and the "show window contents while dragging" setting all work, and nothing is repainted per mouse move.
        /// Only a single left-button press starts a drag: double-clicks and the other buttons reach the control as usual.
        /// While Windows drags the form the control receives no MouseUp or Click for that press, so use passive surfaces as handles.
        /// Calling it again on the same control does nothing more; <see cref="DisableDrag"/> removes it.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="handle"/> is null.</exception>
        public static void EnableDrag(this System.Windows.Forms.Control handle)
        {
            if (handle is null)
            {
                throw new ArgumentNullException(nameof(handle));
            }
            // removing first keeps a single subscription however often this is called
            handle.MouseDown -= NativeDrag_MouseDown;
            handle.MouseDown += NativeDrag_MouseDown;
        }

        /// <summary>
        /// Stops the control from moving its form (undoes <see cref="EnableDrag"/>; does nothing if it was not enabled).
        /// </summary>
        /// <param name="handle">Control passed to <see cref="EnableDrag"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="handle"/> is null.</exception>
        public static void DisableDrag(this System.Windows.Forms.Control handle)
        {
            if (handle is null)
            {
                throw new ArgumentNullException(nameof(handle));
            }
            handle.MouseDown -= NativeDrag_MouseDown;
        }

        // Hand a single left-button press to the native move loop of the form (returns when the drag ends)
        private static void NativeDrag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != Left || e.Clicks > 1 || sender is not System.Windows.Forms.Control { IsDisposed: false } ctrl || ctrl.FindForm() is not { IsDisposed: false } frm)
            {
                return;
            }
            _ = ReleaseCapture();
            _ = SendMessage(frm.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
        #endregion

        #region Form Move
        private const double DRAG_OPACITY = 0.7;

        // The drag started by MoveFrm_MouseDown on this thread (one mouse, one drag); null when none is active
        [ThreadStatic]
        private static MoveDrag _drag;

        /// <summary>
        /// Focus control dùng để di chuyển form.
        /// </summary>
        /// <remarks>
        /// 1.0 handler kept for existing code; <see cref="EnableDrag"/> replaces the three MoveFrm handlers with one call.
        /// Only the left button starts a move. The form is dimmed to 0.7 opacity while it moves and gets its previous opacity back when
        /// the button is released, the control loses the mouse capture or the form is deactivated.
        /// </remarks>
        [EditorBrowsable(Never)]
        public static void MoveFrm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e?.Button != Left || sender is not System.Windows.Forms.Control ctrl || ctrl.FindForm() is not { IsDisposed: false } frm)
            {
                return;
            }
            EndMove();
            var drag = new MoveDrag(ctrl, frm, e.Location, frm.Opacity);
            _drag = drag;
            ctrl.MouseCaptureChanged += MoveCtrl_MouseCaptureChanged;
            ctrl.Disposed += MoveTarget_Lost;
            frm.Deactivate += MoveTarget_Lost;
            frm.Disposed += MoveTarget_Lost;
            frm.Opacity = DRAG_OPACITY;
        }

        /// <summary>
        /// Di chuyển control.
        /// </summary>
        /// <remarks>
        /// 1.0 handler kept for existing code (see <see cref="MoveFrm_MouseDown"/>). A move without the left button held ends the drag
        /// instead of moving the form.
        /// </remarks>
        [EditorBrowsable(Never)]
        public static void MoveFrm_MouseMove(object sender, MouseEventArgs e)
        {
            if (_drag is not { } drag || !ReferenceEquals(sender, drag.Ctrl) || e is null)
            {
                return;
            }
            if ((e.Button & Left) == 0)
            {
                // the button was released where this control could not see it
                EndMove();
                return;
            }
            var frm = drag.Frm;
            frm.Location = new Point(frm.Location.X - drag.Start.X + e.X, frm.Location.Y - drag.Start.Y + e.Y);
        }

        /// <summary>
        /// Kết thúc di chyển.
        /// </summary>
        /// <remarks>
        /// 1.0 handler kept for existing code (see <see cref="MoveFrm_MouseDown"/>). Releasing another button does not end the drag.
        /// </remarks>
        [EditorBrowsable(Never)]
        public static void MoveFrm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e?.Button == Left && _drag is { } drag && ReferenceEquals(sender, drag.Ctrl))
            {
                EndMove();
            }
        }

        // The drag control lost the mouse capture (Alt+Tab, a message box...) before its MouseUp
        private static void MoveCtrl_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (_drag is { } drag && ReferenceEquals(sender, drag.Ctrl) && !drag.Ctrl.Capture)
            {
                EndMove();
            }
        }

        // The form was deactivated, or the form or control disposed, during the drag
        private static void MoveTarget_Lost(object sender, EventArgs e) => EndMove();

        // End the active drag: unhook it and give the form its opacity back (unless something else changed it meanwhile)
        private static void EndMove()
        {
            if (_drag is not { } drag)
            {
                return;
            }
            _drag = null;
            drag.Ctrl.MouseCaptureChanged -= MoveCtrl_MouseCaptureChanged;
            drag.Ctrl.Disposed -= MoveTarget_Lost;
            drag.Frm.Deactivate -= MoveTarget_Lost;
            drag.Frm.Disposed -= MoveTarget_Lost;
            if (!drag.Frm.IsDisposed && !drag.Frm.Disposing && Abs(drag.Frm.Opacity - DRAG_OPACITY) < 0.01)
            {
                drag.Frm.Opacity = drag.Opacity;
            }
        }
        #endregion

        #region Nested types
        // One MoveFrm drag: the pressed control, its form, the press point (client coordinates) and the opacity before the dim
        private sealed class MoveDrag
        {
            public MoveDrag(System.Windows.Forms.Control ctrl, Form frm, Point start, double opacity)
            {
                Ctrl = ctrl;
                Frm = frm;
                Start = start;
                Opacity = opacity;
            }

            public System.Windows.Forms.Control Ctrl { get; }

            public Form Frm { get; }

            public Point Start { get; }

            public double Opacity { get; }
        }
        #endregion
    }
}
