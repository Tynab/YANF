using System.Threading;
using System.Windows.Forms;
using YANF.Screen;

namespace YANF.Script.Service
{
    public class YANWaitScrService : IYANSrcService
    {
        #region Fields
        private YANWaitScreen _waitScr;
        private Thread _thread;
        #endregion

        #region Properties
        public int Corner { get; set; } = 0;
        public bool IsTop { get; set; } = false;
        #endregion

        #region Methods
        // Loading process
        private void LoadingPrc(object parent)
        {
            _waitScr = new YANWaitScreen((Form)parent, Corner, IsTop);
            _ = _waitScr.ShowDialog();
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
            if (_waitScr != null)
            {
                _ = _waitScr.BeginInvoke(new ThreadStart(_waitScr.Frm_Close));
                _waitScr = null;
                _thread = null;
            }
        }
        #endregion
    }
}
