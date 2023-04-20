using System;
using static System.Math;

namespace YANF.Control;

public partial class YANCirPic
{
    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e) => _borderSize = Min(_borderSize, (Width > Height ? Height : Width) / 2);
}
