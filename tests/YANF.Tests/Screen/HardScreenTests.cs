using System;
using System.ComponentModel;
using System.Windows.Forms;
using Xunit;
using YANF.Screen;
using static System.Windows.Forms.Keys;

namespace YANF.Tests.Screen
{
    // HardScreen swallowed Alt+F4 for every subclass; BlockAltF4 keeps that as the default and lets a form opt out
    public class HardScreenTests
    {
        [Fact]
        public void BlockAltF4_DefaultsToTrue_AndSwallowsAltF4() => Sta.Run(() =>
        {
            using var frm = new Probe();
            Assert.True(frm.BlockAltF4);
            Assert.True(frm.Press(Alt | F4));
            // other keys go through as before
            Assert.False(frm.Press(F4));
            Assert.False(frm.Press(Alt | F5));
            Assert.False(frm.Press(Keys.Control | F4));
        });

        [Fact]
        public void BlockAltF4_False_LetsAltF4Through() => Sta.Run(() =>
        {
            using var frm = new Probe { BlockAltF4 = false };
            Assert.False(frm.Press(Alt | F4));
            frm.BlockAltF4 = true;
            Assert.True(frm.Press(Alt | F4));
        });

        [Fact]
        public void BlockAltF4_IsNotSerializedByDefault() => Sta.Run(() =>
        {
            using var frm = new SoftScreen();
            var prop = TypeDescriptor.GetProperties(frm)[nameof(HardScreen.BlockAltF4)];
            Assert.Equal(true, ((DefaultValueAttribute)prop.Attributes[typeof(DefaultValueAttribute)]).Value);
            Assert.False(prop.ShouldSerializeValue(frm));
            frm.BlockAltF4 = false;
            Assert.True(prop.ShouldSerializeValue(frm));
        });

        private sealed class Probe : HardScreen
        {
            // WM_SYSKEYDOWN, as Windows sends Alt+F4
            public bool Press(Keys keys)
            {
                var msg = Message.Create(IntPtr.Zero, 0x104, (IntPtr)(int)(keys & KeyCode), IntPtr.Zero);
                return ProcessCmdKey(ref msg, keys);
            }
        }
    }
}
