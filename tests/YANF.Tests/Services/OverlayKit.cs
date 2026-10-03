using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using static System.Windows.Forms.FormStartPosition;

namespace YANF.Tests.Services
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// Helpers for the overlay services and their internal <c>YANOverlayHost&lt;T&gt;</c>, whose state is private.
    /// </summary>
    internal static class OverlayKit
    {
        #region Methods
        /// <summary>
        /// A parent form that is never shown; the services only read its bounds.
        /// </summary>
        public static Form NewParent() => new()
        {
            StartPosition = Manual,
            Bounds = new Rectangle(100, 80, 640, 480)
        };

        /// <summary>
        /// Runs a service test on an STA thread with the cross-thread check on, then requires every overlay to be closed.
        /// </summary>
        public static void Guarded(Action body) => Sta.Run(() =>
        {
            var oldCheck = Control.CheckForIllegalCrossThreadCalls;
            Control.CheckForIllegalCrossThreadCalls = true;
            try
            {
                body();
                AssertAllClosed("after the test");
            }
            finally
            {
                Control.CheckForIllegalCrossThreadCalls = oldCheck;
            }
        });

        /// <summary>
        /// The service's current host (its private _host field), null when none is active.
        /// </summary>
        public static object HostOf(object service) => Priv.Field<object>(service, "_host");

        /// <summary>
        /// True once the host's overlay thread has ended (or the host was closed before it started).
        /// </summary>
        public static bool Finished(object host) => Locked(host, () => Priv.Field<bool>(host, "_is_Finished"));

        /// <summary>
        /// The loaded screen of a host (null before Load and after the overlay thread ended).
        /// </summary>
        public static Form ScreenOf(object host) => Locked(host, () => Priv.Field<Form>(host, "_scr"));

        /// <summary>
        /// The hidden window of the overlay thread that receives the marshalled calls.
        /// </summary>
        public static Control MailboxOf(object host) => Locked(host, () => Priv.Field<Control>(host, "_mailbox"));

        /// <summary>
        /// Requires the hosts' overlay threads to end and no overlay screen to be left in Application.OpenForms.
        /// </summary>
        public static void AssertAllClosed(string what, params object[] hosts)
        {
            foreach (var h in hosts.Where(h => h != null))
            {
                Assert.True(Poll.Until(() => Finished(h)), what + ": overlay thread did not finish");
            }
            Assert.True(Poll.Until(() => OpenOverlays().Length == 0), what + ": overlay still open: " + string.Join(", ", OpenOverlays().Select(f => f.GetType().Name)));
        }

        /// <summary>
        /// Snapshot of the overlay screens in Application.OpenForms (other threads add and remove forms while it is read).
        /// </summary>
        public static Form[] OpenOverlays()
        {
            for (var i = 0; ; i++)
            {
                try
                {
                    return Application.OpenForms.Cast<Form>().Where(f => f is YANLoadScreen or YANWaitScreen or YANUpdateScreen).Where(f => !f.IsDisposed).ToArray();
                }
                catch (Exception ex) when ((ex is InvalidOperationException or ArgumentOutOfRangeException) && i < 50)
                {
                    Thread.Sleep(5);
                }
            }
        }

        /// <summary>
        /// Reads a value on the overlay's own thread.
        /// </summary>
        public static TR OnOverlay<TR>(Form scr, Func<TR> read)
        {
            // Invoke only marshals once the window reports that it belongs to another thread
            Assert.True(Poll.Until(() => scr.InvokeRequired, 5000), "the overlay window is not owned by the overlay thread");
            return (TR)scr.Invoke(read);
        }

        /// <summary>
        /// Records every text change, size change and disposal of the screen and its controls that does not run on the overlay's own thread.
        /// </summary>
        public static List<string> Watch(Form scr)
        {
            var bad = new List<string>();
            var owner = OnOverlay(scr, () => Thread.CurrentThread.ManagedThreadId);
            Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, owner);
            void Check(string what)
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                if (id != owner)
                {
                    lock (bad)
                    {
                        bad.Add($"{what} on thread {id} (overlay thread {owner})");
                    }
                }
            }
            void Hook(Control c)
            {
                c.TextChanged += (s, e) => Check(c.Name + ".TextChanged");
                c.SizeChanged += (s, e) => Check(c.Name + ".SizeChanged");
                c.Disposed += (s, e) => Check(c.Name + ".Disposed");
                foreach (Control child in c.Controls)
                {
                    Hook(child);
                }
            }
            scr.Invoke(new Action(() => Hook(scr)));
            return bad;
        }

        /// <summary>
        /// Fails when <see cref="Watch"/> recorded a cross-thread access.
        /// </summary>
        public static void AssertAffinity(List<string> bad)
        {
            lock (bad)
            {
                Assert.True(bad.Count == 0, "cross-thread access: " + string.Join(" | ", bad.Take(5)));
            }
        }

        // Reads host state under its lock
        private static TR Locked<TR>(object host, Func<TR> read)
        {
            lock (Priv.Field<object>(host, "_sync"))
            {
                return read();
            }
        }
        #endregion
    }
}
