using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using YANF.Script;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.InterpolationMode;
using static System.Drawing.Drawing2D.PixelOffsetMode;
using static System.Drawing.Drawing2D.WrapMode;
using static System.Windows.Forms.DialogResult;
using static System.Windows.Forms.MessageBoxButtons;
using static System.Windows.Forms.MessageBoxIcon;
using static YANF.Properties.Resources;
using static YANF.Script.YANConstant;
using static YANF.Script.YANConstant.MsgBoxLang;
using static YANF.Script.YANEvent;

namespace YANF.Screen
{
    /// <summary>
    /// The message box form that <see cref="YANMessageBox"/> shows (internal since 2.0).
    /// </summary>
    internal partial class YANMessageBoxScreen : SoftScreen
    {
        #region Fields
        // Layout constants in 96-dpi pixels (scaled to the box's DPI where they are used)
        private const int BTN_MARGIN = 10;
        private const int DFLT_BORDER = 2;
        private const int LINE_HEIGHT = 17;
        private const int MULTI_LINE_BOTTOM = 15;
        private const int WM_DPICHANGED = 0x02E0;

        // Buttons of each set, left to right
        private static readonly Dictionary<MessageBoxButtons, DialogResult[]> _btnSets = new()
        {
            [MessageBoxButtons.OK] = [DialogResult.OK],
            [OKCancel] = [DialogResult.OK, Cancel],
            [AbortRetryIgnore] = [Abort, Retry, Ignore],
            [YesNoCancel] = [Yes, No, Cancel],
            [YesNo] = [Yes, No],
            [RetryCancel] = [Retry, Cancel]
        };

        // Fonts of each language: caption, message, buttons (a language without a row keeps the designer fonts)
        private static readonly Dictionary<MsgBoxLang, (string Family, float Size)[]> _langFnts = new()
        {
            [VIE] = [("Tahoma", 10), ("Segoe UI Light", 9.5f), ("Verdana", 10)],
            [JAP] = [("Yu Gothic", 12), ("Meiryo", 8), ("Meiryo", 9)]
        };

        private Font[] _fntsLang; // created for this box only, disposed with it
        private Color _primaryColor = CornflowerBlue;
        private MessageBoxIcon _icon;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates the message box that <paramref name="options"/> describes. <see cref="YANMessageBox.Show(IWin32Window, YANMessageBoxOptions)"/>
        /// shows it and disposes it.
        /// </summary>
        /// <param name="options">What the box shows.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <exception cref="InvalidEnumArgumentException"><see cref="YANMessageBoxOptions.Buttons"/> is not a <see cref="MessageBoxButtons"/> value.</exception>
        public YANMessageBoxScreen(YANMessageBoxOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (!_btnSets.TryGetValue(options.Buttons, out var results))
            {
                throw new InvalidEnumArgumentException(nameof(options), (int)options.Buttons, typeof(MessageBoxButtons));
            }
            InitializeComponent();
            // the 96-dpi design at the box's DPI before the layout below reads the sizes
            this.ScaleToDpi();
            InitializeItems();
            // prop
            var lang = options.Language ?? ENG;
            SetFntLang(lang);
            PrimaryColor = _primaryColor;
            TopMost = options.TopMost;
            lblMessage.MaximumSize = new Size(SystemInformation.WorkingArea.Width / 2, 0); // long text wraps
            lblCaption.Text = options.Caption;
            lblMessage.Text = options.Text;
            SetSize(results.Length);
            SetBtns(results, options.Buttons, options.DefaultButton, lang, options.StrictClose);
            SetIcon(options.Icon);
            ScaleIcon();
        }
        #endregion

        #region Properties
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // set from the icon, never by the designer
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

        #region Overridden
        /// <summary>
        /// After WinForms has rescaled the box for the DPI of another monitor (per-monitor DPI awareness), redraws what it does not
        /// scale: the default-button border and the icon.
        /// </summary>
        /// <param name="m">The message.</param>
        /// <remarks>WM_DPICHANGED rather than an OnDpiChanged override, whose parameter type is missing from mono's WinForms.</remarks>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_DPICHANGED && !IsDisposed)
            {
                RescaleForDpi();
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            var img = disposing ? picIcon?.Image : null; // read before the picture box is disposed
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
            if (disposing)
            {
                // what this box created for itself (the designer's fonts belong to the designer code)
                img?.Dispose();
                if (_fntsLang != null)
                {
                    Array.ForEach(_fntsLang, f => f.Dispose());
                    _fntsLang = null;
                }
            }
        }
        #endregion

        #region Events
        // Close
        private void BtnClose_Click(object sender, EventArgs e) => Close();

        // Btn got focus: Enter now presses the focused btn (Form.UpdateDefaultButton), so the border moves to it
        private void Btn_GotFocus(object sender, EventArgs e) => MarkBtn(sender);
        #endregion

        #region Methods
        // Initialize items
        private void InitializeItems()
        {
            // move frm by pnl
            pnlHeader.MouseDown += MoveFrm_MouseDown;
            pnlHeader.MouseMove += MoveFrm_MouseMove;
            pnlHeader.MouseUp += MoveFrm_MouseUp;
            // move frm by lbl
            lblCaption.MouseDown += MoveFrm_MouseDown;
            lblCaption.MouseMove += MoveFrm_MouseMove;
            lblCaption.MouseUp += MoveFrm_MouseUp;
            // option
            btnClose.DialogResult = Cancel;
            btn1.DialogResult = DialogResult.OK;
            btn1.Visible = false;
            btn2.Visible = false;
            btn3.Visible = false;
            // the border marks the btn that Enter presses: the default btn, then the focused one (✕ focused: none)
            foreach (var btn in new[] { btn1, btn2, btn3, btnClose })
            {
                btn.GotFocus += Btn_GotFocus;
            }
        }

        // Set language font (created once for this box; a language without fonts of its own keeps the designer fonts)
        private void SetFntLang(MsgBoxLang lang)
        {
            if (!_langFnts.TryGetValue(lang, out var specs))
            {
                return;
            }
            _fntsLang = Array.ConvertAll(specs, s => new Font(s.Family, s.Size));
            lblCaption.Font = _fntsLang[0];
            lblMessage.Font = _fntsLang[1];
            btn1.Font = _fntsLang[2];
            btn2.Font = _fntsLang[2];
            btn3.Font = _fntsLang[2];
        }

        // Set size
        private void SetSize(int btnCount)
        {
            var w = lblMessage.Width + picIcon.Width + pnlBody.Padding.Left;
            var min = btn1.Width * btnCount + this.LogicalToDevice(BTN_MARGIN) * (btnCount + 1) + Padding.Left + Padding.Right;
            w = Math.Max(Math.Max(w, min), lblCaption.Width + btnClose.Width + Padding.Left + Padding.Right);
            if (lblMessage.Height > this.LogicalToDevice(LINE_HEIGHT) + lblMessage.Padding.Top + lblMessage.Padding.Bottom)
            {
                // multi-line: the side padding goes, the wrap width stays (same line breaks, so the text still fits the width above)
                lblMessage.MaximumSize = new Size(Math.Max(1, lblMessage.MaximumSize.Width - lblMessage.Padding.Horizontal), 0);
                lblMessage.Padding = new Padding(0, 0, 0, this.LogicalToDevice(MULTI_LINE_BOTTOM));
            }
            var h = pnlHeader.Height + lblMessage.Height + pnlFooter.Height + pnlBody.Padding.Top + Padding.Top + Padding.Bottom;
            Size = new Size(w, h);
        }

        // Set btns: the buttons of the set centered in the footer, then the default (Enter), Esc and close buttons
        private void SetBtns(DialogResult[] results, MessageBoxButtons btns, MessageBoxDefaultButton btnDflt, MsgBoxLang lang, bool isStrictClose)
        {
            var slots = new[] { btn1, btn2, btn3 };
            var n = results.Length;
            var margin = this.LogicalToDevice(BTN_MARGIN);
            var gap = n == 2 ? margin * 2 : margin;
            var xCtr = (pnlFooter.Width - btn1.Width) / 2;
            var yCtr = (pnlFooter.Height - btn1.Height) / 2;
            var x = xCtr - (n - 1) * (btn1.Width + gap) / 2;
            for (var i = 0; i < n; i++)
            {
                slots[i].Visible = true;
                slots[i].Location = new Point(x + i * (btn1.Width + gap), yCtr);
                slots[i].Text = GetMsgBoxBtnText(lang, btns, results[i]);
                slots[i].DialogResult = results[i];
                slots[i].BackColor = GetBtnColor(results[i]);
            }
            // default btn: a requested btn that the box does not show falls back to btn 1
            var iDflt = (int)btnDflt >> 8;
            var dflt = slots[iDflt >= 0 && iDflt < n ? iDflt : 0];
            MarkBtn(dflt);
            AcceptButton = dflt;
            ActiveControl = dflt;
            // Esc: the Cancel btn, else the OK btn of an OK-only box, else none (Windows message box rule)
            var iCancel = Array.IndexOf(results, Cancel);
            CancelButton = iCancel >= 0 ? slots[iCancel] : btns == MessageBoxButtons.OK ? btn1 : null;
            // close btn: Cancel (1.0), or strictly the Esc btn's result and hidden without one
            if (isStrictClose)
            {
                btnClose.DialogResult = CancelButton?.DialogResult ?? DialogResult.None;
                btnClose.Visible = CancelButton != null;
            }
        }

        // Mark btn: a white border on btn, none on the other btns
        private void MarkBtn(object btn)
        {
            var border = this.LogicalToDevice(DFLT_BORDER);
            foreach (var slot in new[] { btn1, btn2, btn3 })
            {
                slot.FlatAppearance.BorderColor = White;
                slot.FlatAppearance.BorderSize = slot == btn ? border : 0;
            }
        }

        // Get btn color
        private static Color GetBtnColor(DialogResult res) => res switch
        {
            Cancel => DimGray,
            No or Ignore => IndianRed,
            Abort => Goldenrod,
            _ => SeaGreen
        };

        // Set icon: its image and its accent color
        private void SetIcon(MessageBoxIcon icon)
        {
            _icon = icon;
            switch (icon)
            {
                case Error:
                {
                    // error
                    PrimaryColor = FromArgb(224, 79, 95);
                    btnClose.FlatAppearance.MouseOverBackColor = Crimson;
                    break;
                }
                case Information:
                {
                    // information
                    PrimaryColor = FromArgb(38, 191, 166);
                    break;
                }
                case Question:
                {
                    // question
                    PrimaryColor = FromArgb(10, 119, 232);
                    break;
                }
                case Warning:
                {
                    // warning
                    PrimaryColor = FromArgb(255, 140, 0);
                    break;
                }
                case MessageBoxIcon.None:
                {
                    // none: the designer's chat image is already shown
                    PrimaryColor = CornflowerBlue;
                    return;
                }
            }
            SetImage(GetIconImage(icon));
        }

        // Get icon image: a new bitmap of the icon at 96 dpi (the chat image without an icon, and for a value that is not an icon)
        private static Bitmap GetIconImage(MessageBoxIcon icon) => icon switch
        {
            Error => pMessError,
            Information => pMessInfomation,
            Question => pMessQuestion,
            Warning => pMessWarning,
            _ => pMessChat
        };

        // Set image (each resource read is a new bitmap that only this box uses)
        private void SetImage(Image img)
        {
            var old = picIcon.Image;
            picIcon.Image = img;
            old?.Dispose();
        }

        // Scale icon: the images are drawn for 96 dpi and the picture box shows them at their pixel size (nothing changes at 96 dpi).
        // Bicubic, with the edge pixels mirrored outwards: a plain resize blends the opaque edges of the images with transparency
        private void ScaleIcon()
        {
            if (picIcon.Image is not { } img)
            {
                return;
            }
            var size = new Size(this.LogicalToDevice(img.Width), this.LogicalToDevice(img.Height));
            if (size == img.Size || size.Width <= 0 || size.Height <= 0)
            {
                return;
            }
            var bmp = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            try
            {
                using var g = Graphics.FromImage(bmp);
                using var attrs = new ImageAttributes();
                g.InterpolationMode = HighQualityBicubic;
                g.PixelOffsetMode = HighQuality;
                attrs.SetWrapMode(TileFlipXY);
                g.DrawImage(img, new Rectangle(Point.Empty, size), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attrs);
            }
            catch
            {
                bmp.Dispose();
                throw;
            }
            SetImage(bmp);
        }

        // Rescale for DPI: WinForms has scaled the sizes, the fonts and the layout; the border of the marked btn and the icon image are
        // scaled here (the icon again from its 96-dpi image)
        private void RescaleForDpi()
        {
            MarkBtn(Array.Find(new[] { btn1, btn2, btn3 }, b => b.FlatAppearance.BorderSize > 0));
            SetImage(GetIconImage(_icon));
            ScaleIcon();
        }
        #endregion
    }
}
