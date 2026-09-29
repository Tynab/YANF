using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using YANF.Screen;
using static System.Windows.Forms.FormWindowState;

namespace YANF.Script.Service
{
    public class YANUpdScrService : IYANDlvScrService
    {
        #region Fields
        private YANOverlayHost<YANUpdateScreen> _host;
        #endregion

        #region Methods
        // Implementation OnLoader
        public void OnLoader(Form pFrm)
        {
            // Snapshot on the calling thread: the box is centred on pFrm (on the screen, as before, when there is no usable pFrm)
            Rectangle? bounds = pFrm != null && pFrm.WindowState != Minimized ? pFrm.Bounds : null;
            var host = new YANOverlayHost<YANUpdateScreen>(() => bounds.HasValue ? new YANUpdateScreen(bounds.Value) : new YANUpdateScreen());
            Interlocked.Exchange(ref _host, host)?.Close();
            host.Start();
        }

        // Implementation OffLoader
        public void OffLoader() => Interlocked.Exchange(ref _host, null)?.Close();

        // Implementation UpdateValue
        public void PublishValue(int percent, string capacity, int width) => Volatile.Read(ref _host)?.Publish(s =>
        {
            s.lblPercent.Text = $"{percent}%";
            s.pnlProgressBar.Width = width;
            s.lblCapacity.Text = capacity;
        });
        #endregion
    }
}