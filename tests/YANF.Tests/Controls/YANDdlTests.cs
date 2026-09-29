using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANDdlTests
    {
        // 1.0.2 tested the old value in the setter, so Simple slipped through and later values were ignored
        [Fact]
        public void DropDownStyle_SimpleIsIgnored() => Sta.Run(ui =>
        {
            var d = new YANDdl();
            Assert.Equal(ComboBoxStyle.DropDown, d.DropDownStyle);
            d.DropDownStyle = ComboBoxStyle.Simple;
            Assert.Equal(ComboBoxStyle.DropDown, d.DropDownStyle);
            d.DropDownStyle = ComboBoxStyle.DropDownList;
            Assert.Equal(ComboBoxStyle.DropDownList, d.DropDownStyle);
            d.DropDownStyle = ComboBoxStyle.DropDown;
            Assert.Equal(ComboBoxStyle.DropDown, d.DropDownStyle);
            ui.DrawAndDispose(d);
        });

        // A [DefaultValue] that differs from the constructor makes the designer drop real values (1.0.2: AutoCompleteMode None was lost)
        [Theory]
        [InlineData(nameof(YANDdl.AutoCompleteMode))]
        [InlineData(nameof(YANDdl.AutoCompleteSource))]
        [InlineData(nameof(YANDdl.DropDownStyle))]
        public void DefaultValue_MatchesConstructor(string name) => Sta.Run(() =>
        {
            using var d = new YANDdl();
            var p = TypeDescriptor.GetProperties(d)[name];
            var dv = p.Attributes.OfType<DefaultValueAttribute>().FirstOrDefault();
            Assert.NotNull(dv);
            Assert.Equal(dv.Value, p.GetValue(d));
            Assert.False(p.ShouldSerializeValue(d), name + " is serialized at its constructor value");
        });

        [Fact]
        public void AutoCompleteMode_None_IsSerialized() => Sta.Run(() =>
        {
            using var d = new YANDdl { AutoCompleteMode = AutoCompleteMode.None };
            Assert.True(TypeDescriptor.GetProperties(d)[nameof(YANDdl.AutoCompleteMode)].ShouldSerializeValue(d));
        });

        [Fact]
        public void Resize_KeepsBorderSize() => Sta.Run(ui =>
        {
            var d = new YANDdl { BorderSize = 12, MinimumSize = Size.Empty };
            d.Size = new Size(10, 10);
            d.Size = new Size(200, 35);
            Assert.Equal(12, d.BorderSize);
            d.BorderSize = -4;
            Assert.Equal(0, d.BorderSize);
            Assert.Equal(new Padding(0), d.Padding);
            ui.DrawAndDispose(d);
        });
    }
}
