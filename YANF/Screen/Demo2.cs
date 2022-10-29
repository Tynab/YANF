using System;
using System.Windows.Forms;
using YANF.Control;
using YANF.Script;
using static YANF.Properties.Resources;
using static YANF.Script.YANEvent;

namespace YANF.Screen
{
    public partial class Demo2 : Form
    {
        #region Constructors
        public Demo2()
        {
            InitializeComponent();
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

        // btn Back mouse enter
        private void BtnB_MouseEnter(object sender, EventArgs e) => ((Button)sender).BackgroundImage = pBO;

        // btn Quit mouse enter
        private void BtnQ_MouseEnter(object sender, EventArgs e) => ((Button)sender).BackgroundImage = pQO;

        // btn Back mouse leave
        private void BtnB_MouseLeave(object sender, EventArgs e) => ((Button)sender).BackgroundImage = pBO;

        // btn Quit mouse leave
        private void BtnQ_MouseLeave(object sender, EventArgs e) => ((Button)sender).BackgroundImage = pQI;

        // btn Back click
        private void BtnB_Click(object sender, EventArgs e) => Close();

        // btn Quit click
        private void BtnQ_Click(object sender, EventArgs e) => Close();
        #endregion
    }
}
