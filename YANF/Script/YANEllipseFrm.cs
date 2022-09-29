using System;
using System.Runtime.InteropServices;

namespace YANF.Script
{
    public static class YANEllipseFrm
    {
        /// <summary>
        /// Tạo khung ellipse cho form.
        /// </summary>
        /// <param name="nLRect">Tọa độ trái.</param>
        /// <param name="nTRect">Tọa độ trên.</param>
        /// <param name="nRRect">Tọa độ phải.</param>
        /// <param name="nBRect">Tọa độ dưới.</param>
        /// <param name="nWEllipse">Độ rộng.</param>
        /// <param name="nHElippse">Độ cao.</param>
        /// <returns>Platform specific.</returns>
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(int nLRect, int nTRect, int nRRect, int nBRect, int nWEllipse, int nHElippse);
    }
}
