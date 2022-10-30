using System.Threading;
using System.Windows.Forms;
using YANF.Screen;

namespace YANF.Script.Service
{
    public class YANLoadScrService : IYANDlvScrService
    {
        #region Fields
        private YANLoadScreen _loadScr;
        private Thread _thread;
        private Label _lblPercent;
        #endregion

        #region Properties
        public int Corner { get; set; } = 0;
        public bool IsTop { get; set; } = false;
        #endregion

        #region Methods
        // Loading process
        private void LoadingPrc(object parent)
        {
            _loadScr = new YANLoadScreen((Form)parent, Corner, IsTop);
            _lblPercent = _loadScr.lblPercent;
            _ = _loadScr.ShowDialog();
        }

        // Implementation OnLoader
        public void OnLoader(Form pFrm)
        {
            _thread = new Thread(new ParameterizedThreadStart(LoadingPrc));
            _thread.Start(pFrm);
        }

        // Implementation OffLoader
        public void OffLoader()
        {
            if (_loadScr != null)
            {
                _ = _loadScr.BeginInvoke(new ThreadStart(_loadScr.Frm_Close));
                _loadScr = null;
                _thread = null;
            }
        }

        // Implementation UpdateValue
        public void PublishValue(int percent, string capacity, int width) => _lblPercent.Text = $"{percent}%";
        #endregion
    }
}
