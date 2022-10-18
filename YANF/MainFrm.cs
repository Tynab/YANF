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
using YANF.Script.Model;
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
    private int _percent;
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
        _updateScrService = new YANUpdateScrService();
        _percent = 0;
        _updateScrService.OnLoader();
        tmrMain.StartAdv();
    }

    // Timer main
    private void TmrMain_Tick(object sender, EventArgs e)
    {
        if (_percent < 100)
        {
            _percent++;
            var updateScr = new UpdateScr(string.Format("{0} MB / {1} MB", _percent * 1.37, 100 * 1.37), $"{_percent}%", (int)Ceiling(_percent * W_UPDATE_SCR / 100d));
            _ = Invoke((MethodInvoker)delegate
            {
                _updateScrService.PublishValue(updateScr);
            });
        }
        else
        {
            _updateScrService.OffLoader();
            tmrMain.StopAdv();
        }
    }
    #endregion
}
