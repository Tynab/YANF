using System.Threading;
using System.Windows.Forms;
using YANF.Screen;

namespace YANF.Script.Service
{
    public class YANUpdateScrService : IYANUpdScrService
    {
        #region Fields
        private YANUpdateScreen _scrUpd;
        private Thread _thread;
        private Panel _pnlPrg;
        private Label _lblCapacity;
        private Label _lblPercent;
        #endregion

        #region Methods
        // Loading process
        private void LoadingPrc(object parent)
        {
            _scrUpd = new YANUpdateScreen();
            _pnlPrg = _scrUpd.pnlProgressBar;
            _lblCapacity = _scrUpd.lblCapacity;
            _lblPercent = _scrUpd.lblPercent;
            _ = _scrUpd.ShowDialog();
        }

        // Implementation OnLoader
        public void OnLoader()
        {
            _thread = new Thread(new ParameterizedThreadStart(LoadingPrc));
            _thread.Start();
        }

        // Implementation OffLoader
        public void OffLoader()
        {
            if (_scrUpd != null)
            {
                _ = _scrUpd.BeginInvoke(new ThreadStart(_scrUpd.Frm_Close));
                _scrUpd = null;
                _thread = null;
            }
        }

        // Implementation UpdateValue
        public void PublishValue(string capacity, string percent, int width)
        {
            _pnlPrg.Width = width;
            _lblCapacity.Text = capacity;
            _lblPercent.Text = percent;
        }
        #endregion
    }
}