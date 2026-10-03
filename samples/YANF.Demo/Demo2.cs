using System;
using System.Drawing;
using System.Windows.Forms;
using YANF.Screen;
using YANF.Script;
using static YANF.Demo.Properties.Resources;

namespace YANF.Demo
{
    // A YANForm: a borderless window with rounded corners (drawn by Windows 11, a region before) and a drop shadow. It is not
    // resizable (Resizable = false in the designer: the layout has a fixed size), and its passive surfaces act as its title bar
    public partial class Demo2 : YANForm
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
            // move frm by pnl, pic and lbl: YANForm caption controls, which move the form like a title bar (yangradpnl and yancirpic
            // too: GetAllObjs<T> also finds the types derived from T)
            foreach (var pnl in this.GetAllObjs<Panel>())
            {
                RegisterCaptionControl(pnl);
            }
            foreach (var pic in this.GetAllObjs<PictureBox>())
            {
                RegisterCaptionControl(pic);
            }
            foreach (var lbl in this.GetAllObjs<Label>())
            {
                RegisterCaptionControl(lbl);
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
