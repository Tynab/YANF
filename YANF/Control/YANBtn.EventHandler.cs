using System;
using static System.Math;

namespace YANF.Control;

public partial class YANBtn
{
    // Background color changed
    private void Container_BackColorChanged(object sender, EventArgs e) => Invalidate();

    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e)
    {
        var minSize = Width > Height ? Height : Width;
        _borderRadius = Min(_borderRadius, minSize / 2);
        _borderSize = Min(_borderSize, minSize / 2);
    }
}
