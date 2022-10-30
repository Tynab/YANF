using System;
using System.Windows.Forms;
using YANF.Screen;
using YANF.Script;
using YANF.Script.Service;
using static System.Math;
using static YANF.Script.YANConstant;

namespace YANF
{
    public partial class MainFrm : Form
    {
        #region Fields
        private IYANSrcService _srcService;
        private IYANDlvScrService _dlvScrService;
        private string _choosenOne;
        private int _percent;
        #endregion

        #region Constructors
        public MainFrm() => InitializeComponent();
        #endregion

        #region Events
        // Show update screen
        private void BtnUpdScr_Click(object sender, EventArgs e)
        {
            if (!tmrMain.Enabled)
            {
                _choosenOne = "Update";
                _percent = 0;
                _dlvScrService = new YANUpdScrService();
                _dlvScrService.OnLoader(this);
                tmrMain.StartAdv();
            }
        }

        // Show wait screen
        private void BtnWaitScr_Click(object sender, EventArgs e)
        {
            if (!tmrMain.Enabled)
            {
                _choosenOne = "Wait";
                _percent = 0;
                _srcService = new YANWaitScrService();
                _srcService.OnLoader(this);
                this.FadeOut();
                tmrMain.StartAdv();
            }
        }

        // Show load screen
        private void BtnLoadScr_Click(object sender, EventArgs e)
        {
            if (!tmrMain.Enabled)
            {
                _choosenOne = "Load";
                _percent = 0;
                _dlvScrService = new YANLoadScrService();
                _dlvScrService.OnLoader(this);
                this.FadeOut();
                tmrMain.StartAdv();
            }
        }

        // Show demo 1 screen
        private void BtnDemo1_Click(object sender, EventArgs e) => new Demo1().Show();

        // Show demo 2 screen
        private void BtnDemo2_Click(object sender, EventArgs e) => new Demo2().Show();

        // Timer main
        private void TmrMain_Tick(object sender, EventArgs e)
        {
            switch (_choosenOne)
            {
                case "Update":
                {
                    if (_percent < 100)
                    {
                        _percent++;
                        _ = Invoke((MethodInvoker)delegate
                        {
                            _dlvScrService.PublishValue(_percent, string.Format("{0} MB / {1} MB", _percent * 1.37, 100 * 1.37), (int)Ceiling(_percent * W_UPDATE_SCR / 100d));
                        });
                    }
                    else
                    {
                        _dlvScrService.OffLoader();
                        tmrMain.StopAdv();
                    }
                    break;
                }
                case "Wait":
                {
                    if (_percent < 100)
                    {
                        _percent++;
                    }
                    else
                    {
                        _srcService.OffLoader();
                        tmrMain.StopAdv();
                        this.FadeIn();
                    }
                    break;
                }
                case "Load":
                {
                    if (_percent < 100)
                    {
                        _percent++;
                        _ = Invoke((MethodInvoker)delegate
                        {
                            _dlvScrService.PublishValue(_percent, null, 0);
                        });
                    }
                    else
                    {
                        _dlvScrService.OffLoader();
                        tmrMain.StopAdv();
                        this.FadeIn();
                    }
                    break;
                }
            }
        }
        #endregion
    }
}