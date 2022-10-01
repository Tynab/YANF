using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YANF.Script.Service;
using static System.Math;
using static YANF.Script.YANConstant;

namespace YANF.Script;

public class YANEvent
{
    private YANUpdateScrService _updateScrService;
    public YANEvent(YANUpdateScrService srcService)
    {
        _updateScrService = srcService;
    }
    /// <summary>
    /// Truyền hiển thị cho form updater.
    /// </summary>
    public void Updater_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        _updateScrService._lbl_Capacity_.InvokeText(string.Format("{0} MB / {1} MB", (e.BytesReceived / 1024d / 1024d).ToString("0.00"), (e.TotalBytesToReceive / 1024D / 1024D).ToString("0.00")));
        _updateScrService._lbl_Percent_.InvokeText($"{e.ProgressPercentage}%");
        _updateScrService._pnl_Prg_.InvokeW((int)Ceiling(e.ProgressPercentage * W_UPDATE_SCR / 100d));
    }
}
