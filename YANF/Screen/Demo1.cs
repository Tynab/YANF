﻿using System;
using System.Linq;
using System.Windows.Forms;
using YANF.Control;
using YANF.Script;
using static System.DateTime;
using static System.Drawing.Color;
using static System.Windows.Forms.MessageBoxButtons;
using static System.Windows.Forms.MessageBoxIcon;
using static YANF.Properties.Resources;
using static YANF.Script.YANConstant.MsgBoxLang;
using static YANF.Script.YANEvent;
using Label = System.Windows.Forms.Label;

namespace YANF.Screen
{
    public partial class Demo1 : Form
    {
        #region Fields
        private const int _maxMenu = 6;
        private int _activeMenu = 1;
        #endregion

        #region Constructors
        public Demo1()
        {
            InitializeComponent();
            // move frm by pnl
            foreach (var pnl in this.GetAllObjs(typeof(Panel)))
            {
                pnl.MouseDown += MoveFrm_MouseDown;
                pnl.MouseMove += MoveFrm_MouseMove;
                pnl.MouseUp += MoveFrm_MouseUp;
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
            // lbl highlight link by ddl
            foreach (var ddl in this.GetAllObjs(typeof(YANDdl)))
            {
                ddl.Enter += DdlLinkLbl_Enter;
                ddl.Leave += DdlLinkLbl_Leave;
            }
            // lbl highlight link by dp
            foreach (var dp in this.GetAllObjs(typeof(YANDp)))
            {
                dp.Enter += DpLinkLbl_Enter;
                dp.Leave += DpLinkLbl_Leave;
            }
            // lbl highlight link by txt
            foreach (var txt in this.GetAllObjs(typeof(YANTxt)))
            {
                txt.Enter += TxtLinkLbl_Enter;
                txt.Leave += TxtLinkLbl_Leave;
            }
            // btn menu event
            var btns = new Button[_maxMenu]
            {
                btnMenu1,
                btnMenu2,
                btnMenu3,
                btnMenu4,
                btnMenu5,
                btnMenu6
            };
            foreach (var btn in btns)
            {
                btn.MouseEnter += ButtonMenu_MouseEnter;
                btn.MouseLeave += BtnMenu_MouseLeave;
                btn.Click += BtnMenu_Click;
            }
            // set
            pnlMain.Select();
        }
        #endregion

        #region Event
        // Shown frm
        private void Demo1_Shown(object sender, EventArgs e) => this.FadeIn();

        // Closing frm
        private void Demo1_FormClosing(object sender, FormClosingEventArgs e) => this.FadeOut();

        // btn menu mouse enter
        private void ButtonMenu_MouseEnter(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            var currentMenu = btn.Name.Substring("btnMenu".Length).ParseInt();
            if (currentMenu != _activeMenu)
            {
                btn.ForeColor = Yellow;
            }
        }

        // btn Exit mouse enter
        private void BtnExit_MouseEnter(object sender, EventArgs e) => ((Button)sender).BackgroundImage = pXO;

        // Button menu mouse leave
        private void BtnMenu_MouseLeave(object sender, EventArgs e) => ((Button)sender).ForeColor = DodgerBlue;

        // btn Exit mouse leave
        private void BtnExit_MouseLeave(object sender, EventArgs e) => ((Button)sender).BackgroundImage = pXI;

        // btn menu click
        private void BtnMenu_Click(object sender, EventArgs e)
        {
            var currentMenu = ((Button)sender).Name.Substring("btnMenu".Length).ParseInt();
            if (currentMenu != _activeMenu)
            {
                var btnOld = (Button)Controls.Find($"btnMenu{_activeMenu}", true).FirstOrDefault();
                var btnNew = (Button)Controls.Find($"btnMenu{currentMenu}", true).FirstOrDefault();
                btnOld.BackColor = FromArgb(24, 30, 54);
                btnOld.FlatAppearance.MouseOverBackColor = FromArgb(36, 41, 63);
                btnNew.BackColor = FromArgb(46, 51, 73);
                btnNew.FlatAppearance.MouseOverBackColor = FromArgb(46, 51, 73);
                pnlSelected.Top = btnNew.Top;
                lblCaption.Text = btnNew.Text.Substring(12);
                _activeMenu = currentMenu;
            }
        }
        // btn Back click
        private void BtnBack_Click(object sender, EventArgs e) => Close();

        // btn Exit click
        private void BtnExit_Click(object sender, EventArgs e)
        {
            if (YANMessageBox.Show("WARNING", "If you close this window, all data will be lost!", OKCancel, Warning) == DialogResult.OK)
            {
                Close();
            }
        }

        // btn Eraser click
        private void BtnEraser_Click(object sender, EventArgs e) => YANMessageBox.Show("LỖI", "Thất bại!", AbortRetryIgnore, Error, VIE);

        // btn Save click
        private void BtnSave_Click(object sender, EventArgs e) => YANMessageBox.Show("情報", "完了！", OK, Information, JAP);

        // btn Sync click
        private void BtnSync_Click(object sender, EventArgs e) => YANMessageBox.Show("QUESTION", "Do you want to continue?", YesNoCancel, Question);

        // btn Search click
        private void BtnSearch_Click(object sender, EventArgs e) => YANMessageBox.Show("KẾT QUẢ", "Không tìm thấy kết quả!", RetryCancel, VIE);

        // Highlight lbl link by ddl
        private void DdlLinkLbl_Enter(object sender, EventArgs e) => ((YANDdl)sender).HighLightLblLinkByCtrl("ddl", LightYellow, false);

        // Highlight lbl link by dp
        private void DpLinkLbl_Enter(object sender, EventArgs e) => ((YANDp)sender).HighLightLblLinkByCtrl("dp", LightYellow, false);

        // Highlight lbl link by txt
        private void TxtLinkLbl_Enter(object sender, EventArgs e) => ((YANTxt)sender).HighLightLblLinkByCtrl("txt", LightYellow, false);

        // Restored highlight lbl link by ddl
        private void DdlLinkLbl_Leave(object sender, EventArgs e) => ((YANDdl)sender).HighLightLblLinkByCtrl("ddl", WhiteSmoke, false);

        // Restored highlight lbl link by dp
        private void DpLinkLbl_Leave(object sender, EventArgs e) => ((YANDp)sender).HighLightLblLinkByCtrl("dp", WhiteSmoke, false);

        // Restored highlight lbl link by txt
        private void TxtLinkLbl_Leave(object sender, EventArgs e) => ((YANTxt)sender).HighLightLblLinkByCtrl("txt", WhiteSmoke, false);

        // dp NgS value changed
        private void DpNgS_ValueChanged(object sender, EventArgs e)
        {
            var yy = Today.Year - dpNgS.Value.Year;
            lblT.Text = yy > 0 ? $"{yy} tuổi" : null;
        }
        #endregion
    }
}
