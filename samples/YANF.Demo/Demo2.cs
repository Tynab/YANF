using System;
using System.Drawing;
using System.Windows.Forms;
using YANF.Control;
using YANF.Script;
using static YANF.Demo.Properties.Resources;
using static YANF.Script.YANEvent;

namespace YANF.Demo
{
    public partial class Demo2 : Form
    {
        #region Fields
        // btn images, loaded once (every Resources getter call allocates a new Bitmap)
        // normal images come from InitializeComponent, hover images are loaded here
        private readonly Bitmap _pBI;
        private readonly Bitmap _pBO = pBO;
        private readonly Bitmap _pQI;
        private readonly Bitmap _pQO = pQO;
        #endregion

        #region Constructors
        public Demo2()
        {
            InitializeComponent();
            // reuse the normal images the designer already loaded
            _pBI = (Bitmap)btnBack.BackgroundImage;
            _pQI = (Bitmap)btnQuit.BackgroundImage;
            // dispose cached images with frm
            Disposed += Demo2_Disposed;
            // move frm by pnl
            foreach (var pnl in this.GetAllObjs(typeof(Panel)))
            {
                pnl.MouseDown += MoveFrm_MouseDown;
                pnl.MouseMove += MoveFrm_MouseMove;
                pnl.MouseUp += MoveFrm_MouseUp;
            }
            // move frm by pic
            foreach (var pic in this.GetAllObjs(typeof(PictureBox)))
            {
                pic.MouseDown += MoveFrm_MouseDown;
                pic.MouseMove += MoveFrm_MouseMove;
                pic.MouseUp += MoveFrm_MouseUp;
            }
            // move frm by lbl
            foreach (var lbl in this.GetAllObjs(typeof(Label)))
            {
                // without yanddl
                if (lbl.Parent is not YANDdl)
                {
                    lbl.MouseDown += MoveFrm_MouseDown;
                    lbl.MouseMove += MoveFrm_MouseMove;
                    lbl.MouseUp += MoveFrm_MouseUp;
                }
            }
            // move frm by yangradpnl
            foreach (var pnl in this.GetAllObjs(typeof(YANGradPnl)))
            {
                pnl.MouseDown += MoveFrm_MouseDown;
                pnl.MouseMove += MoveFrm_MouseMove;
                pnl.MouseUp += MoveFrm_MouseUp;
            }
            // move frm by yancirpic
            foreach (var pic in this.GetAllObjs(typeof(YANCirPic)))
            {
                pic.MouseDown += MoveFrm_MouseDown;
                pic.MouseMove += MoveFrm_MouseMove;
                pic.MouseUp += MoveFrm_MouseUp;
            }
        }
        #endregion

        #region Events
        // Shown frm
        private void Demo2_Shown(object sender, EventArgs e) => this.FadeIn();

        // Closing frm
        private void Demo2_FormClosing(object sender, FormClosingEventArgs e) => this.FadeOut();

        // Disposed frm (after its controls, so no button still paints the images)
        private void Demo2_Disposed(object sender, EventArgs e)
        {
            _pBI.Dispose();
            _pBO.Dispose();
            _pQI.Dispose();
            _pQO.Dispose();
        }

        // btn Back mouse enter
        private void BtnB_MouseEnter(object sender, EventArgs e) => ((Button)sender).BackgroundImage = _pBO;

        // btn Quit mouse enter
        private void BtnQ_MouseEnter(object sender, EventArgs e) => ((Button)sender).BackgroundImage = _pQO;

        // btn Back mouse leave
        private void BtnB_MouseLeave(object sender, EventArgs e) => ((Button)sender).BackgroundImage = _pBI;

        // btn Quit mouse leave
        private void BtnQ_MouseLeave(object sender, EventArgs e) => ((Button)sender).BackgroundImage = _pQI;

        // btn Back click
        private void BtnB_Click(object sender, EventArgs e) => Close();

        // btn Quit click
        private void BtnQ_Click(object sender, EventArgs e) => Close();
        #endregion
    }
}
