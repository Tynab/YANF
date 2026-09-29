using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.FormStartPosition;

namespace YANF.Tests
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// The WinForms side of one <see cref="Sta.Run(Action{Ui})"/> body: a hidden host form for parentless controls,
    /// drawing helpers, shown forms that are disposed with the scope, and the exceptions that WinForms routed to
    /// Application.ThreadException on this thread (an exception thrown inside a window procedure, for example in OnPaint
    /// during DrawToBitmap or in OnHandleCreated, does not reach the caller on .NET Framework).
    /// </summary>
    internal sealed class Ui
    {
        #region Fields
        private readonly List<Exception> _errors = new();
        private readonly List<Form> _forms = new();
        private Form _host;
        #endregion

        #region Constructors
        private Ui()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Hidden form (never shown) that parentless controls are added to, like a form under construction in the designer.
        /// </summary>
        public Form Host => _host ??= Track(new Form
        {
            ShowInTaskbar = false
        });
        #endregion

        #region Methods
        // Runs one test body on the current (STA) thread, then disposes its forms and reports every failure
        internal static void Run(Action<Ui> body)
        {
            var ui = new Ui();
            ThreadExceptionEventHandler handler = (_, e) => ui.Record(e.Exception);
            Application.ThreadException += handler;
            Exception failure = null;
            try
            {
                body(ui);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            try
            {
                ui.DisposeForms();
            }
            catch (Exception ex)
            {
                failure ??= ex;
            }
            finally
            {
                Application.ThreadException -= handler;
            }
            ui.ThrowIfFailed(failure);
        }

        /// <summary>
        /// Throws when a window procedure of this thread has raised an exception since the last check.
        /// </summary>
        public void ThrowIfFailed() => ThrowIfFailed(null);

        /// <summary>
        /// Runs one case of a sweep and names it in the failure message (including exceptions raised inside window procedures).
        /// </summary>
        public void Case(string name, Action body)
        {
            try
            {
                body();
                ThrowIfFailed();
            }
            catch (Exception ex)
            {
                throw new CaseFailedException(name, ex);
            }
        }

        /// <summary>
        /// Adds a parentless control to <see cref="Host"/>, creates its handle and draws it with DrawToBitmap (WM_PRINT).
        /// </summary>
        public void Draw(Control c)
        {
            using var bmp = Render(c);
        }

        /// <summary>
        /// Draws the control twice (a paint must not change what the next paint sees), then detaches and disposes it.
        /// </summary>
        public void DrawAndDispose(Control c)
        {
            try
            {
                Draw(c);
                Draw(c);
            }
            finally
            {
                c.Parent?.Controls.Remove(c);
                c.Dispose();
            }
        }

        /// <summary>
        /// Draws the control without giving it a parent (its handle is parked by WinForms).
        /// </summary>
        public void DrawDetached(Control c)
        {
            using var bmp = new Bitmap(Math.Max(1, c.Width), Math.Max(1, c.Height));
            Paint(c, bmp);
        }

        /// <summary>
        /// Like <see cref="Draw"/>, but returns the rendered bitmap (the caller disposes it).
        /// </summary>
        public Bitmap Render(Control c)
        {
            if (c.Parent == null)
            {
                Host.Controls.Add(c);
            }
            c.CreateControl();
            var bmp = new Bitmap(Math.Max(1, c.Width), Math.Max(1, c.Height));
            try
            {
                Paint(c, bmp);
                return bmp;
            }
            catch
            {
                bmp.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Shows a form at the top-left of the screen with the controls stacked in it, so focus and on-screen painting are real.
        /// The form is top-most (nothing covers it for screen captures) and is disposed with the scope.
        /// </summary>
        public Form Show(params Control[] controls)
        {
            var frm = Track(new Form
            {
                ShowInTaskbar = false,
                StartPosition = Manual,
                Location = Point.Empty,
                Size = new Size(400, 300),
                TopMost = true
            });
            var y = 10;
            foreach (var c in controls)
            {
                c.Location = new Point(10, y);
                y += c.Height + 10;
                frm.Controls.Add(c);
            }
            frm.Show();
            frm.Activate();
            Pump();
            return frm;
        }

        /// <summary>
        /// Processes pending window messages for a short while (bounded: about 150 ms).
        /// </summary>
        public void Pump(int rounds = 10)
        {
            for (var i = 0; i < rounds; i++)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }
            ThrowIfFailed();
        }

        // Keeps a form for disposal at the end of the scope
        private Form Track(Form frm)
        {
            _forms.Add(frm);
            return frm;
        }

        // DrawToBitmap (WM_PRINT through the real window procedure). It cannot capture a zero-size window on .NET Framework
        // (it throws creating its intermediate bitmap), so a zero-size control is painted by calling its paint handlers directly
        private void Paint(Control c, Bitmap bmp)
        {
            _ = c.Handle;
            ThrowIfFailed();
            if (c.Width > 0 && c.Height > 0)
            {
                c.DrawToBitmap(bmp, new Rectangle(Point.Empty, bmp.Size));
            }
            else
            {
                using var g = Graphics.FromImage(bmp);
                using var painter = new Painter();
                painter.Run(c, g, new Rectangle(Point.Empty, bmp.Size));
            }
            ThrowIfFailed();
        }

        private void Record(Exception ex)
        {
            lock (_errors)
            {
                _errors.Add(ex);
            }
        }

        private void DisposeForms()
        {
            foreach (var frm in _forms.AsEnumerable().Reverse())
            {
                frm.Dispose();
            }
            _forms.Clear();
        }

        private void ThrowIfFailed(Exception failure)
        {
            Exception[] errors;
            lock (_errors)
            {
                errors = _errors.ToArray();
                _errors.Clear();
            }
            if (errors.Length > 0)
            {
                throw new UiThreadException(errors, failure);
            }
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
        #endregion

        #region Nested types
        /// <summary>
        /// Calls the protected paint handlers of another control (Control.InvokePaintBackground / InvokePaint).
        /// </summary>
        private sealed class Painter : Control
        {
            public void Run(Control target, Graphics g, Rectangle clip)
            {
                using (var e = new PaintEventArgs(g, clip))
                {
                    InvokePaintBackground(target, e);
                }
                using (var e = new PaintEventArgs(g, clip))
                {
                    InvokePaint(target, e);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// One or more exceptions that WinForms reported through Application.ThreadException on the test's thread.
    /// </summary>
    internal sealed class UiThreadException : Exception
    {
        public UiThreadException(IReadOnlyList<Exception> errors, Exception bodyFailure)
            : base(Describe(errors, bodyFailure), errors[0])
        {
        }

        private static string Describe(IReadOnlyList<Exception> errors, Exception bodyFailure)
        {
            var text = $"{errors.Count} exception(s) raised in a window procedure: " + string.Join(" | ", errors.Select(e => e.GetType().Name + ": " + e.Message));
            return bodyFailure == null ? text : text + $" (the test body then failed with {bodyFailure.GetType().Name}: {bodyFailure.Message})";
        }
    }

    /// <summary>
    /// A failed case of a sweep, named after the case.
    /// </summary>
    internal sealed class CaseFailedException : Exception
    {
        public CaseFailedException(string name, Exception inner) : base($"{name} -> {inner.GetType().Name}: {inner.Message}", inner)
        {
        }
    }
}
