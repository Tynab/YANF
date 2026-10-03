using System;
using System.Threading;
using System.Windows.Forms;
using YANF.Screen;

namespace YANF.Script.Service
{
    public class YANLoadScrService : IYANDlvScrService
    {
        #region Fields
        private YANOverlayHost<YANLoadScreen> _host;
        #endregion

        #region Properties
        public int Corner { get; set; } = 0;
        public bool IsTop { get; set; } = false;
        #endregion

        #region Methods
        // Implementation OnLoader
        public void OnLoader(Form pFrm)
        {
            if (pFrm == null)
            {
                throw new ArgumentNullException(nameof(pFrm));
            }
            // Snapshot on the calling thread: the screen is built on its own thread
            var bounds = pFrm.Bounds;
            var corner = Corner;
            var isTop = IsTop;
            var host = new YANOverlayHost<YANLoadScreen>(() => new YANLoadScreen(bounds, corner, isTop));
            Interlocked.Exchange(ref _host, host)?.Close();
            host.Start();
        }

        // Implementation OffLoader
        public void OffLoader() => Interlocked.Exchange(ref _host, null)?.Close();

        // Implementation UpdateValue
        public void PublishValue(int percent, string capacity, int width) => Volatile.Read(ref _host)?.Publish(s => s.SetProgress(percent, capacity));
        #endregion
    }
}
