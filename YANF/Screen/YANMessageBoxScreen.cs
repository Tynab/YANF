using System;
using System.Drawing;
using System.Windows.Forms;
using YANF.Script;
using static System.Drawing.Color;
using static System.Drawing.FontStyle;
using static System.Windows.Forms.DialogResult;
using static System.Windows.Forms.MessageBoxButtons;
using static System.Windows.Forms.MessageBoxDefaultButton;
using static System.Windows.Forms.MessageBoxIcon;
using static YANF.Properties.Resources;
using static YANF.Script.YANConstant;
using static YANF.Script.YANConstant.MsgBoxLang;
using static YANF.Script.YANEvent;
using static YANF.Script.YANMath;

namespace YANF.Screen
{
    public partial class YANMessageBoxScreen : Form
    {
        #region Fields
        private readonly Font _fntTitVn = new("Tahoma", 10);
        private readonly Font _fntTitJp = new("Yu Gothic", 12);
        private readonly Font _fntCapVn = new("Verdana", 10);
        private readonly Font _fntCapJp = new("Meiryo", 9);
        private readonly Font _fntTextVn = new("Segoe UI Light", 9.5f);
        private readonly Font _fntTextJp = new("Meiryo", 8);
        private Color _primaryColor = CornflowerBlue;
        #endregion

        #region Constructors
        public YANMessageBoxScreen(string text)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            PrimaryColor = _primaryColor;
            lblCaption.Text = null;
            lblMessage.Text = text;
            SetSize(MessageBoxButtons.OK);
            SetBtns(MessageBoxButtons.OK, Button1); // set default btns
        }

        public YANMessageBoxScreen(string text, MsgBoxLang lang)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            SetFntLang(lang);
            PrimaryColor = _primaryColor;
            lblCaption.Text = null;
            lblMessage.Text = text;
            SetSize(MessageBoxButtons.OK);
            // set default btns
            switch (lang)
            {
                case JAP:
                {
                    SetBtnsJp(MessageBoxButtons.OK, Button1);
                    break;
                }
                case VIE:
                {
                    SetBtnsVn(MessageBoxButtons.OK, Button1);
                    break;
                }
            }
        }

        public YANMessageBoxScreen(string cap, string text)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(MessageBoxButtons.OK);
            SetBtns(MessageBoxButtons.OK, Button1); // set default btns
        }

        public YANMessageBoxScreen(string cap, string text, MsgBoxLang lang)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            SetFntLang(lang);
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(MessageBoxButtons.OK);
            // set default btns
            switch (lang)
            {
                case JAP:
                {
                    SetBtnsJp(MessageBoxButtons.OK, Button1);
                    break;
                }
                case VIE:
                {
                    SetBtnsVn(MessageBoxButtons.OK, Button1);
                    break;
                }
            }
        }

        public YANMessageBoxScreen(string cap, string text, MessageBoxButtons btns)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(btns);
            SetBtns(btns, Button1); // set [default btn 1]
        }

        public YANMessageBoxScreen(string cap, string text, MessageBoxButtons btns, MsgBoxLang lang)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            SetFntLang(lang);
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(btns);
            // set [default btn 1]
            switch (lang)
            {
                case JAP:
                {
                    SetBtnsJp(btns, Button1);
                    break;
                }
                case VIE:
                {
                    SetBtnsVn(btns, Button1);
                    break;
                }
            }
        }

        public YANMessageBoxScreen(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(btns);
            SetBtns(btns, Button1);
            SetIcon(icon);
        }

        public YANMessageBoxScreen(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MsgBoxLang lang)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            SetFntLang(lang);
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(btns);
            // set [default btn 1]
            switch (lang)
            {
                case JAP:
                {
                    SetBtnsJp(btns, Button1);
                    break;
                }
                case VIE:
                {
                    SetBtnsVn(btns, Button1);
                    break;
                }
            }
            SetIcon(icon);
        }

        public YANMessageBoxScreen(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(btns);
            SetBtns(btns, btnDflt);
            SetIcon(icon);
        }

        public YANMessageBoxScreen(string cap, string text, MessageBoxButtons btns, MessageBoxIcon icon, MessageBoxDefaultButton btnDflt, MsgBoxLang lang)
        {
            InitializeComponent();
            InitializeItems();
            // btn Close
            btnClose.Click += BtnClose_Click;
            // prop
            SetFntLang(lang);
            PrimaryColor = _primaryColor;
            lblCaption.Text = cap;
            lblMessage.Text = text;
            SetSize(btns);
            switch (lang)
            {
                case JAP:
                {
                    SetBtnsJp(btns, btnDflt);
                    break;
                }
                case VIE:
                {
                    SetBtnsVn(btns, btnDflt);
                    break;
                }
            }
            SetIcon(icon);
        }
        #endregion

        #region Properties
        public Color PrimaryColor
        {
            get => _primaryColor;
            set
            {
                _primaryColor = value;
                BackColor = _primaryColor; // form border color
            }
        }
        #endregion

        #region Events
        // Close
        private void BtnClose_Click(object sender, EventArgs e) => Close();
        #endregion

        #region Methods
        // Initialize items
        private void InitializeItems()
        {
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
                lbl.MouseDown += MoveFrm_MouseDown;
                lbl.MouseMove += MoveFrm_MouseMove;
                lbl.MouseUp += MoveFrm_MouseUp;
            }
            // option
            btnClose.DialogResult = Cancel;
            btn1.DialogResult = DialogResult.OK;
            btn1.Visible = false;
            btn2.Visible = false;
            btn3.Visible = false;
        }

        // Set language font
        private void SetFntLang(MsgBoxLang lang)
        {
            switch (lang)
            {
                case JAP:
                {
                    lblCaption.Font = _fntTitJp;
                    lblMessage.Font = _fntTextJp;
                    btn1.Font = _fntCapJp;
                    btn2.Font = _fntCapJp;
                    btn3.Font = _fntCapJp;
                    break;
                }
                case VIE:
                {
                    lblCaption.Font = _fntTitVn;
                    lblMessage.Font = _fntTextVn;
                    btn1.Font = _fntCapVn;
                    btn2.Font = _fntCapVn;
                    btn3.Font = _fntCapVn;
                    break;
                }
            }
        }

        // Set size
        private void SetSize(MessageBoxButtons btns)
        {
            var w = lblMessage.Width + picIcon.Width + pnlBody.Padding.Left;
            var min = 0;
            switch (btns)
            {
                case MessageBoxButtons.OK:
                {
                    min = btn1.Width + 10 * 2 + +Padding.Left + Padding.Right;
                    break;
                }
                case OKCancel:
                case RetryCancel:
                case YesNo:
                {
                    min = btn1.Width * 2 + 10 * 3 + +Padding.Left + Padding.Right;
                    break;
                }
                default:
                {
                    min = btn1.Width * 3 + 10 * 4 + +Padding.Left + Padding.Right;
                    break;
                }
            }
            w = Max(w, min, lblCaption.Width + btnClose.Width + Padding.Left + Padding.Right);
            if (lblMessage.Height > 17 + lblMessage.Padding.Top + lblMessage.Padding.Bottom)
            {
                lblMessage.Padding = new Padding(0, 0, 0, 15);
            }
            var h = pnlHeader.Height + lblMessage.Height + pnlFooter.Height + pnlBody.Padding.Top + Padding.Top + Padding.Bottom;
            Size = new Size(w, h);
        }

        // Set btns
        private void SetBtns(MessageBoxButtons btns, MessageBoxDefaultButton btnDflt)
        {
            var xCtr = (pnlFooter.Width - btn1.Width) / 2;
            var yCtr = (pnlFooter.Height - btn1.Height) / 2;
            switch (btns)
            {
                case MessageBoxButtons.OK:
                {
                    // ok btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr, yCtr);
                    btn1.Text = "OK";
                    btn1.DialogResult = DialogResult.OK; // set dialogResult
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
                case OKCancel:
                {
                    // ok btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "OK";
                    btn1.DialogResult = DialogResult.OK; // set dialogResult
                    // cancel btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "Cancel";
                    btn2.DialogResult = Cancel; // set dialogResult
                    btn2.BackColor = DimGray;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case RetryCancel:
                {
                    // retry btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "Retry";
                    btn1.DialogResult = Retry; // set dialogResult
                    // cancel btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "Cancel";
                    btn2.DialogResult = Cancel; // set dialogResult
                    btn2.BackColor = DimGray;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case YesNo:
                {
                    // yes btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "Yes";
                    btn1.DialogResult = Yes; //set dialogResult
                    // no btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "No";
                    btn2.DialogResult = No; //set dialogResult
                    btn2.BackColor = IndianRed;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case YesNoCancel:
                {
                    // yes btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width - 10, yCtr);
                    btn1.Text = "Yes";
                    btn1.DialogResult = Yes; // set dialogResult
                    // no btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr, yCtr);
                    btn2.Text = "No";
                    btn2.DialogResult = No; // set dialogResult
                    btn2.BackColor = IndianRed;
                    // cancel btn
                    btn3.Visible = true;
                    btn3.Location = new Point(xCtr + btn2.Width + 10, yCtr);
                    btn3.Text = "Cancel";
                    btn3.DialogResult = Cancel; // set dialogResult
                    btn3.BackColor = DimGray;
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
                case AbortRetryIgnore:
                {
                    // abort btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width - 10, yCtr);
                    btn1.Text = "Abort";
                    btn1.DialogResult = Abort; // set dialogResult
                    btn1.BackColor = Goldenrod;
                    // retry btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr, yCtr);
                    btn2.Text = "Retry";
                    btn2.DialogResult = Retry; // set dialogResult
                    // ignore btn
                    btn3.Visible = true;
                    btn3.Location = new Point(xCtr + btn2.Width + 10, yCtr);
                    btn3.Text = "Ignore";
                    btn3.DialogResult = Ignore; // set dialogResult
                    btn3.BackColor = IndianRed;
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
            }
        }

        // Set btns Vietnamese
        private void SetBtnsVn(MessageBoxButtons btns, MessageBoxDefaultButton btnDflt)
        {
            var xCtr = (pnlFooter.Width - btn1.Width) / 2;
            var yCtr = (pnlFooter.Height - btn1.Height) / 2;
            switch (btns)
            {
                case MessageBoxButtons.OK:
                {
                    // ok btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr, yCtr);
                    btn1.Text = "Xong";
                    btn1.DialogResult = DialogResult.OK; //set dialogResult
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
                case OKCancel:
                {
                    // ok btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "Xong";
                    btn1.DialogResult = DialogResult.OK; // set dialogResult
                    // cancel btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "Hủy";
                    btn2.DialogResult = Cancel; // set dialogResult
                    btn2.BackColor = DimGray;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case RetryCancel:
                {
                    // retry btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "Thử lại";
                    btn1.DialogResult = Retry; // set dialogResult
                    // cancel btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "Hủy";
                    btn2.DialogResult = Cancel; // set dialogResult
                    btn2.BackColor = DimGray;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case YesNo:
                {
                    // yes btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "Vâng";
                    btn1.DialogResult = Yes; // set dialogResult
                    // no btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "Không";
                    btn2.DialogResult = No; // set dialogResult
                    btn2.BackColor = IndianRed;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case YesNoCancel:
                {
                    // yes btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width - 10, yCtr);
                    btn1.Text = "Vâng";
                    btn1.DialogResult = Yes; // set dialogResult
                    // no btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr, yCtr);
                    btn2.Text = "Không";
                    btn2.DialogResult = No; // set dialogResult
                    btn2.BackColor = IndianRed;
                    // cancel btn
                    btn3.Visible = true;
                    btn3.Location = new Point(xCtr + btn2.Width + 10, yCtr);
                    btn3.Text = "Hủy";
                    btn3.DialogResult = Cancel; // set dialogResult
                    btn3.BackColor = DimGray;
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
                case AbortRetryIgnore:
                {
                    // abort btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width - 10, yCtr);
                    btn1.Text = "Hủy Bỏ";
                    btn1.DialogResult = Abort; // set dialogResult
                    btn1.BackColor = Goldenrod;
                    // retry btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr, yCtr);
                    btn2.Text = "Thử lại";
                    btn2.DialogResult = Retry; // set dialogResult
                    // ignore Button
                    btn3.Visible = true;
                    btn3.Location = new Point(xCtr + btn2.Width + 10, yCtr);
                    btn3.Text = "Bỏ qua";
                    btn3.DialogResult = Ignore; // set dialogResult
                    btn3.BackColor = IndianRed;
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
            }
        }

        // Set btns Japanese
        private void SetBtnsJp(MessageBoxButtons btns, MessageBoxDefaultButton btnDflt)
        {
            var xCtr = (pnlFooter.Width - btn1.Width) / 2;
            var yCtr = (pnlFooter.Height - btn1.Height) / 2;
            switch (btns)
            {
                case MessageBoxButtons.OK:
                {
                    // ok btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr, yCtr);
                    btn1.Text = "オーケー";
                    btn1.DialogResult = DialogResult.OK; // set dialogResult
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
                case OKCancel:
                {
                    // ok btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "オーケー";
                    btn1.DialogResult = DialogResult.OK; // set dialogResult
                    // cancel btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "キャンセル";
                    btn2.DialogResult = Cancel; // set dialogResult
                    btn2.BackColor = DimGray;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case RetryCancel:
                {
                    // retry btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "リトライ";
                    btn1.DialogResult = Retry; // set dialogResult
                    // cancel btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "キャンセル";
                    btn2.DialogResult = Cancel; // set dialogResult
                    btn2.BackColor = DimGray;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case YesNo:
                {
                    // yes btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width / 2 - 10, yCtr);
                    btn1.Text = "はい";
                    btn1.DialogResult = Yes; // set dialogResult
                    // no btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr + btn2.Width / 2 + 10, yCtr);
                    btn2.Text = "いいえ";
                    btn2.DialogResult = No; // set dialogResult
                    btn2.BackColor = IndianRed;
                    // set default btn
                    if (btnDflt != Button3) // there are only 2 btns, so the default btn cannot be btn3
                    {
                        SetDefaultBtn(btnDflt);
                    }
                    else
                    {
                        SetDefaultBtn(Button1);
                    }
                    break;
                }
                case YesNoCancel:
                {
                    // yes btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width - 10, yCtr);
                    btn1.Text = "はい";
                    btn1.DialogResult = Yes; // set dialogResult
                    // no btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr, yCtr);
                    btn2.Text = "いいえ";
                    btn2.DialogResult = No; // set dialogResult
                    btn2.BackColor = IndianRed;
                    // cancel btn
                    btn3.Visible = true;
                    btn3.Location = new Point(xCtr + btn2.Width + 10, yCtr);
                    btn3.Text = "キャンセル";
                    btn3.DialogResult = Cancel; // set dialogResult
                    btn3.BackColor = DimGray;
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
                case AbortRetryIgnore:
                {
                    // abort btn
                    btn1.Visible = true;
                    btn1.Location = new Point(xCtr - btn1.Width - 10, yCtr);
                    btn1.Text = "アボート";
                    btn1.DialogResult = Abort; // set dialogResult
                    btn1.BackColor = Goldenrod;
                    // retry btn
                    btn2.Visible = true;
                    btn2.Location = new Point(xCtr, yCtr);
                    btn2.Text = "リトライ";
                    btn2.DialogResult = Retry; // set dialogResult
                    // ignore btn
                    btn3.Visible = true;
                    btn3.Location = new Point(xCtr + btn2.Width + 10, yCtr);
                    btn3.Text = "無視";
                    btn3.DialogResult = Ignore; // set dialogResult
                    btn3.BackColor = IndianRed;
                    // set default btn
                    SetDefaultBtn(btnDflt);
                    break;
                }
            }
        }

        // Set default btn
        private void SetDefaultBtn(MessageBoxDefaultButton btnDflt)
        {
            switch (btnDflt)
            {
                case Button1:
                {
                    // focus btn 1
                    btn1.Select();
                    btn1.ForeColor = White;
                    btn1.Font = new Font(btn1.Font, Underline);
                    break;
                }
                case Button2:
                {
                    // focus btn 2
                    btn2.Select();
                    btn2.ForeColor = White;
                    btn2.Font = new Font(btn2.Font, Underline);
                    break;
                }
                case Button3:
                {
                    // focus btn 3
                    btn3.Select();
                    btn3.ForeColor = White;
                    btn3.Font = new Font(btn3.Font, Underline);
                    break;
                }
            }
        }

        // Set icon
        private void SetIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case Error:
                {
                    // error
                    picIcon.Image = pMessError;
                    PrimaryColor = FromArgb(224, 79, 95);
                    btnClose.FlatAppearance.MouseOverBackColor = Crimson;
                    break;
                }
                case Information:
                {
                    // information
                    picIcon.Image = pMessInfomation;
                    PrimaryColor = FromArgb(38, 191, 166);
                    break;
                }
                case Question:
                {
                    // question
                    picIcon.Image = pMessQuestion;
                    PrimaryColor = FromArgb(10, 119, 232);
                    break;
                }
                case Warning:
                {
                    // warning
                    picIcon.Image = pMessWarning;
                    PrimaryColor = FromArgb(255, 140, 0);
                    break;
                }
                case MessageBoxIcon.None:
                {
                    // none
                    picIcon.Image = pMessChat;
                    PrimaryColor = CornflowerBlue;
                    break;
                }
            }
        }
        #endregion
    }
}
