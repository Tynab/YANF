using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Xunit;
using YANF.Script;
using static System.Windows.Forms.MouseButtons;

namespace YANF.Tests.Script
{
    using Control = System.Windows.Forms.Control;

    // EnableDrag hands the move to Windows (user32 is not callable under mono, so only the paths that stay managed are run here);
    // the 1.0 MoveFrm handlers kept a static drag state that any button started and only MouseUp ended
    public class YANEventTests
    {
        [Fact]
        public void EnableDrag_SubscribesOnce_AndDisableDragRemovesIt() => Sta.Run(() =>
        {
            using var frm = new Form();
            var pnl = new Panel();
            frm.Controls.Add(pnl);
            pnl.EnableDrag();
            pnl.EnableDrag();
            Assert.Equal(1, Handlers.Count(pnl, nameof(Control.MouseDown)));
            frm.EnableDrag();
            frm.EnableDrag();
            Assert.Equal(1, Handlers.Count(frm, nameof(Control.MouseDown)));
            pnl.DisableDrag();
            pnl.DisableDrag();
            Assert.Equal(0, Handlers.Count(pnl, nameof(Control.MouseDown)));
            // a consumer's own handler is left alone
            MouseEventHandler own = (_, _) => { };
            pnl.MouseDown += own;
            pnl.EnableDrag();
            pnl.DisableDrag();
            Assert.Equal(1, Handlers.Count(pnl, nameof(Control.MouseDown)));
            Assert.Throws<ArgumentNullException>(() => ((Control)null).EnableDrag());
            Assert.Throws<ArgumentNullException>(() => ((Control)null).DisableDrag());
        });

        [Fact]
        public void EnableDrag_IgnoresOtherButtons_DoubleClicks_AndParentlessControls() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(50, 50) };
            var frm = ui.Show(pnl);
            var loc = frm.Location;
            pnl.EnableDrag();
            // none of these reaches the native move loop (which would need user32)
            Priv.Call(pnl, "OnMouseDown", new MouseEventArgs(Right, 1, 5, 5, 0));
            Priv.Call(pnl, "OnMouseDown", new MouseEventArgs(Middle, 1, 5, 5, 0));
            Priv.Call(pnl, "OnMouseDown", new MouseEventArgs(Left, 2, 5, 5, 0));
            using var lone = new Panel();
            lone.EnableDrag();
            Priv.Call(lone, "OnMouseDown", new MouseEventArgs(Left, 1, 5, 5, 0));
            ui.Pump(2);
            Assert.Equal(loc, frm.Location);
        });

        [Fact]
        public void MoveFrm_LeftDrag_MovesDimsAndRestores() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(50, 50) };
            var frm = ui.Show(pnl);
            frm.Location = new Point(100, 80);
            frm.Opacity = 0.9;
            YANEvent.MoveFrm_MouseDown(pnl, new MouseEventArgs(Left, 1, 10, 10, 0));
            Assert.Equal(0.7, frm.Opacity, 3);
            YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(Left, 0, 25, 15, 0));
            Assert.Equal(new Point(115, 85), frm.Location);
            YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(Left, 0, 5, 30, 0));
            Assert.Equal(new Point(110, 105), frm.Location);
            // releasing another button does not end the drag
            YANEvent.MoveFrm_MouseUp(pnl, new MouseEventArgs(Right, 1, 5, 30, 0));
            Assert.Equal(0.7, frm.Opacity, 3);
            YANEvent.MoveFrm_MouseUp(pnl, new MouseEventArgs(Left, 1, 5, 30, 0));
            // the opacity the form had before the drag (1.0 set it to 1 whatever it was)
            Assert.Equal(0.9, frm.Opacity, 3);
            // no drag any more: moves are ignored
            YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(Left, 0, 50, 50, 0));
            Assert.Equal(new Point(110, 105), frm.Location);
            ui.ThrowIfFailed();
        });

        [Fact]
        public void MoveFrm_IgnoresOtherButtons() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(50, 50) };
            var frm = ui.Show(pnl);
            frm.Location = new Point(100, 80);
            foreach (var button in new[] { Right, Middle, XButton1, None })
            {
                YANEvent.MoveFrm_MouseDown(pnl, new MouseEventArgs(button, 1, 10, 10, 0));
                Assert.Equal(1, frm.Opacity, 3);
                YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(button, 0, 40, 40, 0));
                YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(Left, 0, 40, 40, 0));
                Assert.Equal(new Point(100, 80), frm.Location);
                YANEvent.MoveFrm_MouseUp(pnl, new MouseEventArgs(button, 1, 40, 40, 0));
                Assert.Equal(1, frm.Opacity, 3);
            }
            // parentless sender, foreign sender, null args: ignored (1.0 threw NullReferenceException)
            using var lone = new Panel();
            YANEvent.MoveFrm_MouseDown(lone, new MouseEventArgs(Left, 1, 1, 1, 0));
            YANEvent.MoveFrm_MouseDown("not a control", new MouseEventArgs(Left, 1, 1, 1, 0));
            YANEvent.MoveFrm_MouseDown(pnl, null);
            YANEvent.MoveFrm_MouseMove(pnl, null);
            YANEvent.MoveFrm_MouseUp(pnl, null);
            Assert.Equal(1, frm.Opacity, 3);
        });

        [Fact]
        public void MoveFrm_MoveWithoutButton_EndsTheDrag() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(50, 50) };
            var other = new Panel { Size = new Size(50, 50) };
            var frm = ui.Show(pnl, other);
            frm.Location = new Point(100, 80);
            YANEvent.MoveFrm_MouseDown(pnl, new MouseEventArgs(Left, 1, 10, 10, 0));
            // moves reported by another control do not move the form
            YANEvent.MoveFrm_MouseMove(other, new MouseEventArgs(Left, 0, 40, 40, 0));
            Assert.Equal(new Point(100, 80), frm.Location);
            // the MouseUp was lost: the next move without the button ends the drag instead of moving the form (1.0 kept dragging)
            YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(None, 0, 40, 40, 0));
            Assert.Equal(new Point(100, 80), frm.Location);
            Assert.Equal(1, frm.Opacity, 3);
            YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(Left, 0, 60, 60, 0));
            Assert.Equal(new Point(100, 80), frm.Location);
        });

        [Fact]
        public void MoveFrm_CaptureLossOrDeactivate_RestoresTheOpacity() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(50, 50) };
            var frm = ui.Show(pnl);
            // capture lost (Alt+Tab, a message box) before the MouseUp
            pnl.Capture = true;
            YANEvent.MoveFrm_MouseDown(pnl, new MouseEventArgs(Left, 1, 10, 10, 0));
            Assert.Equal(0.7, frm.Opacity, 3);
            pnl.Capture = false;
            ui.Pump(2);
            Assert.Equal(1, frm.Opacity, 3);
            // deactivated form
            YANEvent.MoveFrm_MouseDown(pnl, new MouseEventArgs(Left, 1, 10, 10, 0));
            Assert.Equal(0.7, frm.Opacity, 3);
            Priv.Call(frm, "OnDeactivate", EventArgs.Empty);
            Assert.Equal(1, frm.Opacity, 3);
            var loc = frm.Location;
            YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(Left, 0, 40, 40, 0));
            Assert.Equal(loc, frm.Location);
            // the drag unhooked itself from the control and the form
            Assert.Equal(0, Handlers.Count(pnl, nameof(Control.MouseCaptureChanged)));
            Assert.Equal(0, Handlers.Count(frm, nameof(Form.Deactivate)));
            // a new press on another form's control ends the stale drag first
            YANEvent.MoveFrm_MouseDown(pnl, new MouseEventArgs(Left, 1, 10, 10, 0));
            var pnl2 = new Panel { Size = new Size(50, 50) };
            var frm2 = ui.Show(pnl2);
            YANEvent.MoveFrm_MouseDown(pnl2, new MouseEventArgs(Left, 1, 10, 10, 0));
            Assert.Equal(1, frm.Opacity, 3);
            Assert.Equal(0.7, frm2.Opacity, 3);
            YANEvent.MoveFrm_MouseUp(pnl2, new MouseEventArgs(Left, 1, 10, 10, 0));
            Assert.Equal(1, frm2.Opacity, 3);
        });

        [Fact]
        public void MoveFrm_FormDisposedDuringDrag_DoesNotThrow() => Sta.Run(ui =>
        {
            var frm = new Form { ShowInTaskbar = false };
            var pnl = new Panel();
            frm.Controls.Add(pnl);
            frm.Show();
            YANEvent.MoveFrm_MouseDown(pnl, new MouseEventArgs(Left, 1, 10, 10, 0));
            frm.Dispose();
            YANEvent.MoveFrm_MouseMove(pnl, new MouseEventArgs(Left, 0, 40, 40, 0));
            YANEvent.MoveFrm_MouseUp(pnl, new MouseEventArgs(Left, 1, 40, 40, 0));
            ui.Pump(2);
        });
    }

    /// <summary>
    /// The handlers subscribed to an event of a component. The key of an event in Component.Events is a private static field whose
    /// name differs between .NET Framework, .NET and mono, and between the events of one runtime (a guessed name picked a key that
    /// VisibleChanged does not use on .NET Framework 4.8.1), so the key is found instead: a probe handler is subscribed through the
    /// event itself on a blank instance of the type, and the key is the static field of the type hierarchy under which Events stores it.
    /// </summary>
    internal static class Handlers
    {
        private const BindingFlags STATIC = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private static readonly ConcurrentDictionary<(Type, string), object> _keys = new();

        /// <summary>
        /// Number of handlers of the event; with <paramref name="declaredBy"/>, only the handlers whose method that type declares (for
        /// example the ones a YANF helper subscribed, whatever the runtime itself or the test subscribed besides).
        /// </summary>
        public static int Count(Component component, string eventName, Type declaredBy = null) => Of(component, eventName).Count(d => declaredBy == null || d.Method.DeclaringType == declaredBy);

        /// <summary>
        /// The handlers of the event, in the order they are called.
        /// </summary>
        public static Delegate[] Of(Component component, string eventName) => Priv.Property<EventHandlerList>(component, "Events")[Key(component.GetType(), eventName)]?.GetInvocationList() ?? new Delegate[0];

        // The key of the event in Component.Events (the keys are static: found once per type and event). SampleKit calls it by name
        private static object Key(Type type, string eventName) => _keys.GetOrAdd((type, eventName), k => FindKey(k.Item1, k.Item2));

        private static object FindKey(Type type, string eventName)
        {
            var evt = FindEvent(type, eventName) ?? throw new MissingMemberException(type.FullName, eventName);
            // a blank instance: no constructor runs (no window, no synchronization context installed), and the add accessors of the
            // WinForms events only store the handler in Events
            var blank = (Component)Blank(type);
            GC.SuppressFinalize(blank);
            var probe = Delegate.CreateDelegate(evt.EventHandlerType, new Probe(), typeof(Probe).GetMethod(nameof(Probe.Handle)));
            evt.AddEventHandler(blank, probe);
            var list = Priv.Property<EventHandlerList>(blank, "Events");
            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var f in t.GetFields(STATIC))
                {
                    // the probe is the only handler of the blank instance: the one key with an entry is the event's
                    if (!f.FieldType.IsValueType && !f.FieldType.IsPointer && Value(f) is { } key && list[key] != null)
                    {
                        return key;
                    }
                }
            }
            throw new MissingFieldException(type.FullName, "event key of " + eventName);
        }

        // Value of a static field (null for one that cannot be read through reflection)
        private static object Value(FieldInfo f)
        {
            try
            {
                return f.GetValue(null);
            }
            catch (Exception ex) when (ex is NotSupportedException or FieldAccessException or TargetInvocationException)
            {
                return null;
            }
        }

        // The event as the type exposes it (the most derived declaration when a class hides an inherited event)
        private static EventInfo FindEvent(Type type, string eventName)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (t.GetEvent(eventName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) is { } evt)
                {
                    return evt;
                }
            }
            return null;
        }

        // An instance of the type whose constructors have not run
#if NETFRAMEWORK
        private static object Blank(Type type) => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
#else
        private static object Blank(Type type) => System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
#endif

        // Target of the probe handler (bound to any EventHandler-shaped delegate type: its parameters are the base types)
        private sealed class Probe
        {
            public void Handle(object sender, EventArgs e)
            {
            }
        }
    }
}
