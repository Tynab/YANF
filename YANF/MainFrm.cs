using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YANF.Model;
using YANF.Screen;
using YANF.Script;
using YANF.Script.Service;
using static System.Math;
using static YANF.Script.YANConstant;

namespace YANF;

public partial class MainFrm : Form
{
    #region Fields
    private IYANUpdateScrService _updateScrService;
    private int _percent = 0;
    #endregion

    #region Constructors
    public MainFrm()
    {
        InitializeComponent();
    }
    #endregion

    #region Events
    // Show update screen
    private void YanBtnUpdateScr_Click(object sender, EventArgs e)
    {
        var updateScrService = new YANUpdateScrService();
        _updateScrService = updateScrService;
        _updateScrService.OnLoader();
        tmrMain.Start();
    }

    // Timer main
    private void TmrMain_Tick(object sender, EventArgs e)
    {
        if (_percent < 100)
        {
            _percent++;
            var updateScr = new UpdateScr(string.Format("{0} MB / {1} MB", _percent, 100), $"{_percent}%", (int)Ceiling(_percent * W_UPDATE_SCR / 100d));
            _ = Invoke((MethodInvoker)delegate
            {
                _updateScrService.PublishValue(updateScr);
            });
            //_updateScrService.UpdateValue(updateScr);
            //updateScrService._lbl_Capacity_.InvokeText(string.Format("{0} MB / {1} MB", _percent, 100));
            //updateScrService._lbl_Percent_.InvokeText($"{_percent}%");
            //updateScrService._pnl_Prg_.InvokeW((int)Ceiling(_percent * W_UPDATE_SCR / 100d));
        }
        else
        {
            tmrMain.Stop();
        }
    }
    #endregion
}
