using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    using Control = System.Windows.Forms.Control;

    public class YANBtnTests
    {
        [Fact]
        public void BorderLargerThanRadius_Draws() => Sta.Run(ui => ui.DrawAndDispose(new YANBtn
        {
            BorderSize = 25,
            BorderRadius = 20
        }));

        [Fact]
        public void AllSizesAndShapes_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes)
            {
                foreach (var (radius, border) in Sweep.PaintShapes)
                {
                    ui.Case($"YANBtn {size.Width}x{size.Height} r={radius} b={border}", () =>
                    {
                        var b = Sweep.Sized(new YANBtn { BorderRadius = radius, BorderSize = border }, size);
                        Assert.Equal(Math.Max(0, radius), b.BorderRadius);
                        Assert.Equal(Math.Max(0, border), b.BorderSize);
                        ui.DrawAndDispose(b);
                    });
                }
            }
        });

        [Fact]
        public void Resize_KeepsConfiguredValues() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = 20, BorderSize = 5 };
            b.Size = new Size(30, 30);
            b.Size = new Size(4, 4);
            b.Size = new Size(0, 0);
            b.Size = new Size(150, 40);
            Assert.Equal(20, b.BorderRadius);
            Assert.Equal(5, b.BorderSize);
            ui.DrawAndDispose(b);
        });

        [Fact]
        public void NegativeValues_BecomeZero() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = -10, BorderSize = -2 };
            Assert.Equal(0, b.BorderRadius);
            Assert.Equal(0, b.BorderSize);
            ui.DrawAndDispose(b);
        });

        // 1.0.2 threw a NullReferenceException from OnHandleCreated when the button had no parent
        [Fact]
        public void NoParent_CreateAndDraw() => Sta.Run(ui =>
        {
            using var b = new YANBtn { BorderRadius = 20, BorderSize = 3 };
            Assert.Null(b.Parent);
            b.CreateControl();
            _ = b.Handle;
            ui.ThrowIfFailed();
            ui.DrawDetached(b);
            b.BorderRadius = 5;
            b.Size = new Size(20, 10);
            ui.DrawDetached(b);
        });

        // 1.0.2 subscribed to Parent.BackColorChanged in OnHandleCreated: every handle recreation added one more handler
        [Fact]
        public void RecreateHandle_NoParentSubscription() => Sta.Run(ui =>
        {
            var pnl = new Panel { Size = new Size(300, 100) };
            ui.Host.Controls.Add(pnl);
            var b = new YANBtn();
            pnl.Controls.Add(b);
            var handles = 0;
            b.HandleCreated += (s, e) => handles++;
            _ = b.Handle;
            var before = handles;
            // RightToLeft recreates the handle; force it if the runtime does not
            b.RightToLeft = RightToLeft.Yes;
            b.RightToLeft = RightToLeft.No;
            if (handles == before)
            {
                Priv.Call(b, "RecreateHandle");
            }
            Assert.True(handles > before, "the handle was not recreated");
            Assert.DoesNotContain(BackColorHandlers(pnl), d => ReferenceEquals(d.Target, b));
            // the parent's BackColor still reaches the button (OnParentBackColorChanged) without throwing
            pnl.BackColor = Color.Red;
            ui.Draw(b);
            // reparenting must not leave a handler on the old or the new parent either
            var pnl2 = new Panel { Size = new Size(300, 100) };
            ui.Host.Controls.Add(pnl2);
            pnl2.Controls.Add(b);
            Assert.DoesNotContain(BackColorHandlers(pnl).Concat(BackColorHandlers(pnl2)), d => ReferenceEquals(d.Target, b));
        });

        // 1.0.2 assigned a new Region in OnPaint, and assigning a region invalidates the window: an endless repaint loop
        [Fact]
        public void Region_OnlyChangesWithShape() => Sta.Run(ui =>
        {
            var b = new YANBtn { BorderRadius = 20, BorderSize = 2, Size = new Size(150, 40) };
            ui.Draw(b);
            Assert.NotNull(b.Region);
            var changes = 0;
            b.RegionChanged += (s, e) => changes++;
            ui.Draw(b);
            ui.Draw(b);
            Assert.Equal(0, changes);
            b.BorderColor = Color.Blue;
            b.BorderSize = 4;
            Assert.Equal(0, changes);
            b.BorderRadius = 10;
            Assert.Equal(1, changes);
            b.BorderRadius = 10;
            Assert.Equal(1, changes);
            b.Size = new Size(100, 30);
            Assert.Equal(2, changes);
            b.BorderRadius = 0;
            Assert.Null(b.Region);
        });

        // The hand cursor comes from DefaultCursor now; 1.0.2 overwrote Cursor on every mouse move
        [Fact]
        public void UserCursor_NotOverwrittenOnMouseMove() => Sta.Run(() =>
        {
            using var b = new YANBtn { Cursor = Cursors.Cross };
            Priv.Call(b, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));
            Assert.Same(Cursors.Cross, b.Cursor);
        });

        // Every delegate subscribed to one of the parent's BackColor events (event keys looked up by name)
        private static List<Delegate> BackColorHandlers(Control parent)
        {
            var events = Priv.Property<EventHandlerList>(parent, "Events");
            var keys = typeof(Control).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(object) && f.Name.IndexOf("BackColor", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(f => f.GetValue(null))
                .ToList();
            Assert.NotEmpty(keys);
            return keys.Select(k => events[k]).Where(d => d != null).SelectMany(d => d.GetInvocationList()).ToList();
        }
    }
}
