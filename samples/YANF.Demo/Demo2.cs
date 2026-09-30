using System;
using System.Drawing;
using System.Windows.Forms;
using YANF.Script;
using static YANF.Demo.Properties.Resources;

namespace YANF.Demo
{
    public partial class Demo2 : Form
    {
        #region Fields
        private const int FADE_MS = 250;
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
            // fade in when shown and out when closed, without blocking the UI thread
            this.EnableFade(FADE_MS, FADE_MS);
            // reuse the normal images the designer already loaded
            _pBI = (Bitmap)btnBack.BackgroundImage;
            _pQI = (Bitmap)btnQuit.BackgroundImage;
            // dispose cached images with frm
            Disposed += Demo2_Disposed;
            // move frm by pnl (yangradpnl too: GetAllObjs<T> also finds the types derived from T)
            foreach (var pnl in this.GetAllObjs<Panel>())
            {
                pnl.EnableDrag();
            }
            // move frm by pic (yancirpic too)
            foreach (var pic in this.GetAllObjs<PictureBox>())
            {
                pic.EnableDrag();
            }
            // move frm by lbl
            foreach (var lbl in this.GetAllObjs<Label>())
            {
                lbl.EnableDrag();
            }
        }
        #endregion

        #region Events
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
