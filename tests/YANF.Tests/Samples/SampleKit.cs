using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using YANF.Script;
using YANF.Tests.Script;

namespace YANF.Tests.Samples
{
    using Control = System.Windows.Forms.Control;

    /// <summary>
    /// Helpers for the tests of the sample application.
    /// </summary>
    internal static class SampleKit
    {
        #region Methods
        /// <summary>
        /// Every control below <paramref name="root"/> (not the root itself), walked without YANF's own helpers.
        /// </summary>
        public static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var d in Descendants(c))
                {
                    yield return d;
                }
            }
        }

        /// <summary>
        /// How many of the handlers of a control event are YANF's own (YANEvent: EnableDrag, or the 1.0 MoveFrm handlers).
        /// </summary>
        public static int YanEventHandlers(Control c, string eventName)
        {
            var key = Priv.CallStatic(typeof(Handlers), "Key", c.GetType(), eventName);
            var list = Priv.Property<EventHandlerList>(c, "Events");
            return list[key]?.GetInvocationList().Count(d => d.Method.DeclaringType == typeof(YANEvent)) ?? 0;
        }

        /// <summary>
        /// Text of a named part of a message box (lblCaption, lblMessage, btn1...).
        /// </summary>
        public static string Part(YANMessageBoxScreen box, string name) => Priv.Field<Control>(box, name).Text;

        /// <summary>
        /// The buttons that the message box shows.
        /// </summary>
        public static Button[] Buttons(YANMessageBoxScreen box) => new[] { "btn1", "btn2", "btn3" }.Select(n => Priv.Field<Button>(box, n)).Where(b => b.Visible).ToArray();

        /// <summary>
        /// Clicks the button of the message box that gives <paramref name="result"/>.
        /// </summary>
        public static void Press(YANMessageBoxScreen box, DialogResult result) => Buttons(box).Single(b => b.DialogResult == result).PerformClick();
        #endregion
    }

    /// <summary>
    /// Runs an action on each message box that this thread shows (the action must close it, for example with
    /// <see cref="SampleKit.Press"/>). A box still open 10 s later is hidden, which ends its ShowDialog, and <see cref="Check"/>
    /// fails the test instead of letting it hang.
    /// </summary>
    internal sealed class BoxWatcher : IDisposable
    {
        #region Fields
        private const int TIMEOUT_MS = 10000;
        private readonly System.Windows.Forms.Timer _timer = new()
        {
            Interval = 20
        };
        private readonly Stopwatch _sw = new();
        private readonly Action<YANMessageBoxScreen> _act;
        private readonly List<string> _texts = new();
        private YANMessageBoxScreen _box;
        private Exception _error;
        private bool _is_TimedOut;
        #endregion

        #region Constructors
        public BoxWatcher(Action<YANMessageBoxScreen> act)
        {
            _act = act;
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        #endregion

        #region Properties
        /// <summary>
        /// The message of every box seen so far.
        /// </summary>
        public IReadOnlyList<string> Texts => _texts;
        #endregion

        #region Methods
        /// <summary>
        /// Rethrows what the action threw, and fails when <paramref name="expected"/> boxes were not shown or one did not close.
        /// </summary>
        public void Check(int expected)
        {
            if (_error != null)
            {
                ExceptionDispatchInfo.Capture(_error).Throw();
            }
            Assert.False(_is_TimedOut, "a message box did not close");
            Assert.True(expected == _texts.Count, $"{_texts.Count} message box(es) instead of {expected}: " + string.Join(" | ", _texts));
        }

        public void Dispose() => _timer.Dispose();

        // Find the next box of this thread, act on it once, and wait for it to close
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_box == null)
            {
                _box = Application.OpenForms.OfType<YANMessageBoxScreen>().FirstOrDefault(f => !f.IsDisposed && f.Visible && !f.InvokeRequired);
                if (_box == null)
                {
                    return;
                }
                _texts.Add(SampleKit.Part(_box, "lblMessage"));
                _sw.Restart();
                try
                {
                    _act(_box);
                }
                catch (Exception ex)
                {
                    _error ??= ex;
                    _box.Hide();
                }
            }
            else if (_box.IsDisposed || !_box.Visible)
            {
                _box = null;
            }
            else if (_sw.ElapsedMilliseconds > TIMEOUT_MS)
            {
                _is_TimedOut = true;
                _box.Hide();
            }
        }
        #endregion
    }
}
