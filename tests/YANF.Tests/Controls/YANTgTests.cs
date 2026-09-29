using System.Drawing;
using System.Linq;
using Xunit;
using YANF.Control;

namespace YANF.Tests.Controls
{
    public class YANTgTests
    {
        [Fact]
        public void AllSizesAndStates_Draw() => Sta.Run(ui =>
        {
            foreach (var size in Sweep.PaintSizes.Concat(new[] { new Size(45, 22), new Size(22, 45), new Size(2, 2) }))
            {
                foreach (var on in new[] { false, true })
                {
                    foreach (var solid in new[] { false, true })
                    {
                        ui.Case($"YANTg {size.Width}x{size.Height} on={on} solid={solid}", () => ui.DrawAndDispose(Sweep.Sized(new YANTg { Checked = on, SolidStyle = solid }, size)));
                    }
                }
            }
        });

        [Fact]
        public void NoParent_Draws() => Sta.Run(ui =>
        {
            using var t = new YANTg { Checked = true };
            ui.DrawDetached(t);
            t.Checked = false;
            ui.DrawDetached(t);
        });
    }
}
