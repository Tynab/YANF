using System;
using System.Runtime.InteropServices;
using static YANF.Script.YANConstant;

namespace YANF.Script;

public static class YANAnimation
{
    /// <summary>
    /// Điều khiển object với animation đồng bộ.
    /// </summary>
    /// <param name="hwand">Object.</param>
    /// <param name="dwTime">Thời gian tính bằng milisecond.</param>
    /// <param name="dwFlags">Flag animate.</param>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern void AnimateWindow(IntPtr hwand, int dwTime, AnimateWindowFlags dwFlags);
}
