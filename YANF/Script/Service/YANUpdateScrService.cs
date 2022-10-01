using System.Threading;
using System.Windows.Forms;
using YANF.Model;
using YANF.Screen;

namespace YANF.Script.Service;

public class YANUpdateScrService : IYANUpdateScrService
{
    #region Fields
    public Panel _pnl_Prg_;
    public Label _lbl_Capacity_;
    public Label _lbl_Percent_;
    private YANUpdateScr _scr_Update;
    private Thread _thread;
    #endregion

    #region Methods
    // Loading process
    private void LoadingPrc(object parent)
    {
        _scr_Update = new YANUpdateScr();
        _pnl_Prg_ = _scr_Update.panelProgressBar;
        _lbl_Capacity_ = _scr_Update.labelCapacity;
        _lbl_Percent_ = _scr_Update.labelPercent;
        _ = _scr_Update.ShowDialog();
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
        if (_scr_Update != null)
        {
            _ = _scr_Update.BeginInvoke(new ThreadStart(_scr_Update.Frm_Close));
            _scr_Update = null;
            _thread = null;
        }
    }

    // Implementation UpdateValue
    public void PublishValue(UpdateScr updateScr)
    {
        _pnl_Prg_.Width = updateScr.Width;
        _lbl_Capacity_.Text = updateScr.Capacity;
        _lbl_Percent_.Text = updateScr?.Percent;
    }
    #endregion
}
