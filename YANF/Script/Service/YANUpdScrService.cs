using System.Threading;
using System.Windows.Forms;
using YANF.Screen;

namespace YANF.Script.Service
{
    public class YANUpdScrService : IYANDlvScrService
    {
        #region Fields
        private YANUpdateScreen _updScr;
        private Thread _thread;
        private Panel _pnlPrg;
        private Label _lblCapacity;
        private Label _lblPercent;
        #endregion

        #region Methods
        // Loading process
        private void LoadingPrc(object parent)
        {
            _updScr = new YANUpdateScreen();
            _pnlPrg = _updScr.pnlProgressBar;
            _lblCapacity = _updScr.lblCapacity;
            _lblPercent = _updScr.lblPercent;
            _ = _updScr.ShowDialog();
        }

        // Implementation OnLoader
        public void OnLoader(Form pFrm)
        {
            _thread = new Thread(new ParameterizedThreadStart(LoadingPrc));
            _thread.Start();
        }

        // Implementation OffLoader
        public void OffLoader()
        {
            if (_updScr != null)
            {
                _ = _updScr.BeginInvoke(new ThreadStart(_updScr.Frm_Close));
                _updScr = null;
                _thread = null;
            }
        }

        // Implementation UpdateValue
        public void PublishValue(int percent, string capacity, int width)
        {
            _lblPercent.Text = $"{percent}%";
            _pnlPrg.Width = width;
            _lblCapacity.Text = capacity;
        }
        #endregion
    }
}