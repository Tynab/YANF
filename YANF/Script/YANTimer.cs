using System.Windows.Forms;

namespace YANF.Script
{
    public static class YANTimer
    {
        /// <summary>
        /// Chạy timer.
        /// </summary>
        public static void StartAdv(this Timer tmr)
        {
            if (!tmr.Enabled)
            {
                tmr.Start();
            }
        }

        /// <summary>
        /// Dừng timer.
        /// </summary>
        public static void StopAdv(this Timer tmr)
        {
            if (tmr.Enabled)
            {
                tmr.Stop();
            }
        }
    }
}