using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Math;
using static System.Windows.Forms.TextRenderer;
using static YANF.Script.YANShape;

namespace YANF.Control
{
    [DefaultEvent("StringChanged")]
    public class YANTxt : UserControl
    {
        #region Fields
        private Color _borderColor = MediumSlateBlue;
        private Color _placeholderColor = DarkGray;
        private string _placeholderText = null;
        private int _borderSize = 2;
        private int _borderRadius = 0;
        private bool _is_UnderlinedStyle = false;
        private bool _is_Focus = false;
        private readonly CueTextBox _txtText;
        #endregion

        #region Constructors
        public YANTxt()
        {
            _txtText = new CueTextBox();
            SuspendLayout();
            // textbox text
            _txtText.BackColor = White;
            _txtText.BorderStyle = BorderStyle.None;
            _txtText.TextAlign = HorizontalAlignment.Center;
            _txtText.Dock = DockStyle.Fill;
            _txtText.Location = new Point(10, 7);
            _txtText.Size = new Size(180, 18);
            _txtText.Font = new Font(Font.Name, 11f);
            _txtText.CueColor = _placeholderColor;
            _txtText.MouseEnter += Txt_MouseEnter;
            _txtText.MouseLeave += Txt_MouseLeave;
            _txtText.Enter += Txt_Enter;
            _txtText.Leave += Txt_Leave;
            _txtText.KeyDown += Txt_KeyDown;
            _txtText.KeyPress += Txt_KeyPress;
            _txtText.KeyUp += Txt_KeyUp;
            _txtText.TextChanged += Txt_TextChanged;
            _txtText.SizeChanged += Txt_SizeChanged;
            // user control
            Controls.Add(_txtText);
            DoubleBuffered = true;
            ResizeRedraw = true;
            ForeColor = DimGray;
            BackColor = White;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(200, 30);
            Padding = new Padding(10, 7, 10, 7);
            Font = new Font(Font.Name, 11f);
            // base
            ResumeLayout();
        }
        #endregion

        #region Properties
        [Category("YAN Appearance"), Description("Indicates how the text should be aligned for edit controls.")]
        public HorizontalAlignment TextAlign
        {
            get => _txtText.TextAlign;
            set
            {
                if (_txtText.TextAlign != value)
                {
                    _txtText.TextAlign = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("This property specifies the color of the border around the control.")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor != value)
                {
                    _borderColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("This property specifies the color of the border around the control when the control have the focus.")]
        public Color BorderFocusColor { get; set; } = HotPink;

        [Category("YAN Appearance"), Description("The color of the placeholder text.")]
        public Color PlaceholderColor
        {
            get => _placeholderColor;
            set
            {
                if (_placeholderColor != value)
                {
                    _placeholderColor = value;
                    _txtText.CueColor = value;
                }
            }
        }

        [Category("YAN Appearance"), Description("The text associated with the control.")]
        public string String
        {
            // null while the box is unfocused, empty (or whitespace only) and a placeholder is set; the raw text while focused, as in 1.0.2
            get => HasPlaceholder() && !_is_Focus && string.IsNullOrWhiteSpace(_txtText.Text) ? null : _txtText.Text;
            // null or whitespace clears the box when a placeholder is set, as in 1.0.2
            set => _txtText.Text = HasPlaceholder() && string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [Category("YAN Appearance"), Description("The text that is displayed when the control has no text and does not have the focus.")]
        public string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                if (_placeholderText != value)
                {
                    _placeholderText = value;
                    _txtText.Cue = value;
                }
            }
        }

        [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
        public int BorderSize
        {
            get => _borderSize;
            set
            {
                value = Max(0, value);
                if (_borderSize != value)
                {
                    _borderSize = value;
                    UpdateTextRegion();
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("This property allows you to add rounded corners to the control.")]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                value = Max(0, value);
                if (_borderRadius != value)
                {
                    _borderRadius = value;
                    UpdateRegion();
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("Specifies the maximum number of characters that can be entered into the edit control.")]
        public int MaxLength { get => _txtText.MaxLength; set => _txtText.MaxLength = value; }

        [Category("YAN Appearance"), Description("When this property is true, the underline added to text.")]
        public bool UnderlinedStyle
        {
            get => _is_UnderlinedStyle;
            set
            {
                if (_is_UnderlinedStyle != value)
                {
                    _is_UnderlinedStyle = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("Indicates the character to display for password input for single-line edit controls.")]
        public bool PasswordChar { get => _txtText.UseSystemPasswordChar; set => _txtText.UseSystemPasswordChar = value; }

        [Category("YAN Appearance"), Description("Control whether the text of the edit control can span more than one line.")]
        public bool Multiline
        {
            get => _txtText.Multiline;
            set
            {
                if (_txtText.Multiline != value)
                {
                    _txtText.Multiline = value;
                    UpdateTextRegion();
                }
            }
        }

        // Event
        [Category("YAN Event"), Description("Event raised when the value of the Txt property is changed on Control.")]
        public event EventHandler StringChanged;
        #endregion

        #region Overridden
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor = value;
                _txtText.BackColor = value;
            }
        }

        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                base.ForeColor = value;
                _txtText.ForeColor = value;
            }
        }

        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                _txtText.Font = value;
                if (DesignMode)
                {
                    UpdateHCtrl();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            var borderRadius = GetBorderRadius();
            var borderSize = GetBorderSize();
            using var penBorder = new Pen(_is_Focus ? BorderFocusColor : _borderColor, borderSize);
            if (borderRadius > 1)
            {
                var rectBorderSmooth = ClientRectangle;
                using var pathBorderSmooth = RoundedRect(rectBorderSmooth, borderRadius);
                using var pathBorder = RoundedRect(Inflate(rectBorderSmooth, -borderSize, -borderSize), borderRadius - borderSize);
                using var penBorderSmooth = new Pen(Parent?.BackColor ?? BackColor, borderSize > 0 ? borderSize : 1);
                graphics.SmoothingMode = AntiAlias;
                penBorder.Alignment = Center;
                // draw border smoothing
                graphics.DrawPath(penBorderSmooth, pathBorderSmooth);
                if (_is_UnderlinedStyle)
                {
                    // draw border
                    graphics.SmoothingMode = None;
                    graphics.DrawLine(penBorder, 0, Height - 1, Width, Height - 1);
                }
                else if (borderSize >= 1 && pathBorder != null)
                {
                    // draw border
                    graphics.DrawPath(penBorder, pathBorder);
                }
            }
            else
            {
                penBorder.Alignment = Inset;
                if (_is_UnderlinedStyle)
                {
                    graphics.DrawLine(penBorder, 0, Height - 1, Width, Height - 1);
                }
                else if (borderSize >= 1)
                {
                    graphics.DrawRectangle(penBorder, 0, 0, Width - 0.5f, Height - 0.5f);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion();
            if (DesignMode)
            {
                UpdateHCtrl();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateHCtrl();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            // the border smoothing is drawn with the parent back color
            Invalidate();
        }
        #endregion

        #region Events
        // Raises the mouse enter event
        private void Txt_MouseEnter(object sender, EventArgs e) => OnMouseEnter(e);

        // Raises the mouse leave event
        private void Txt_MouseLeave(object sender, EventArgs e) => OnMouseLeave(e);

        // Raises the enter event
        private void Txt_Enter(object sender, EventArgs e)
        {
            _is_Focus = true;
            _txtText.Select(0, _txtText.TextLength);
            Invalidate();
        }

        // Raises the leave event
        private void Txt_Leave(object sender, EventArgs e)
        {
            _is_Focus = false;
            Invalidate();
        }

        // Raises the key press event
        private void Txt_KeyPress(object sender, KeyPressEventArgs e) => OnKeyPress(e);

        // Raises the key down event
        private void Txt_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e);
            if (e.KeyCode == Keys.Enter && !_txtText.Multiline)
            {
                e.SuppressKeyPress = true;
            }
        }

        // Raises the key up event
        private void Txt_KeyUp(object sender, KeyEventArgs e) => OnKeyUp(e);

        // Raises the text changed event
        private void Txt_TextChanged(object sender, EventArgs e) => StringChanged?.Invoke(sender, e);

        // Update the rounded region of the text box when its size changes
        private void Txt_SizeChanged(object sender, EventArgs e) => UpdateTextRegion();
        #endregion

        #region Methods
        // Check whether a placeholder is set
        private bool HasPlaceholder() => !string.IsNullOrWhiteSpace(_placeholderText);

        // Get the border radius that fits the current size (the configured value is never changed)
        private int GetBorderRadius() => (int)EffectiveRadius(ClientRectangle, _borderRadius);

        // Get the border size that fits the current size (the configured value is never changed)
        private int GetBorderSize() => Max(0, Min(_borderSize, Min(Width, Height) / 2));

        // Update the region of the control when its size or radius changes, never while painting
        private void UpdateRegion()
        {
            var borderRadius = GetBorderRadius();
            if (borderRadius > 1)
            {
                using var pathRegion = RoundedRect(ClientRectangle, borderRadius);
                SetRegion(this, pathRegion);
            }
            else
            {
                SetRegion(this, (Region)null);
            }
            UpdateTextRegion();
        }

        // Update the rounded region of the text box, only needed when the corners of the control are big enough to cut it
        private void UpdateTextRegion()
        {
            if (_txtText == null)
            {
                return;
            }
            var borderRadius = GetBorderRadius();
            if (borderRadius > 15)
            {
                var borderSize = GetBorderSize();
                using var pathText = RoundedRect(_txtText.ClientRectangle, _txtText.Multiline ? borderRadius - borderSize : borderSize * 2);
                SetRegion(_txtText, pathText);
            }
            else
            {
                SetRegion(_txtText, (Region)null);
            }
        }

        // Update the height of control when changed font display
        private void UpdateHCtrl()
        {
            if (!_txtText.Multiline)
            {
                _txtText.Multiline = true;
                _txtText.MinimumSize = new Size(0, MeasureText("Text", Font).Height + 1);
                _txtText.Multiline = false;
                Height = _txtText.Height + Padding.Top + Padding.Bottom;
                UpdateTextRegion();
            }
        }
        #endregion

        #region CueTextBox
        // Text box that paints the placeholder over its empty edit area instead of writing the placeholder into Text
        private sealed class CueTextBox : TextBox
        {
            private const int WM_PAINT = 0x000F;
            private string _cue;
            private Color _cueColor;

            // The placeholder text
            internal string Cue
            {
                get => _cue;
                set
                {
                    _cue = value;
                    Invalidate();
                }
            }

            // The color of the placeholder text
            internal Color CueColor
            {
                get => _cueColor;
                set
                {
                    _cueColor = value;
                    Invalidate();
                }
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                // blank text (whitespace only) counts as empty, like the String property and the placeholder of 1.0.2
                if (m.Msg == WM_PAINT && !Focused && !string.IsNullOrWhiteSpace(_cue) && (TextLength == 0 || string.IsNullOrWhiteSpace(Text)))
                {
                    DrawCue();
                }
            }

            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                Invalidate();
            }

            protected override void OnLostFocus(EventArgs e)
            {
                base.OnLostFocus(e);
                Invalidate();
            }

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                // a text set by code while unfocused must not leave the placeholder half painted
                if (!Focused)
                {
                    Invalidate();
                }
            }

            // Draw the placeholder aligned like the text (same layout as the placeholder of the .NET TextBox)
            private void DrawCue()
            {
                var flags = TextFormatFlags.NoPadding | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | (Multiline ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine);
                var rect = ClientRectangle;
                var isRtl = RightToLeft == RightToLeft.Yes;
                if (isRtl)
                {
                    flags |= TextFormatFlags.RightToLeft;
                }
                switch (TextAlign)
                {
                    case HorizontalAlignment.Center:
                        flags |= TextFormatFlags.HorizontalCenter;
                        rect.Offset(0, 1);
                        break;
                    case HorizontalAlignment.Left:
                        flags |= isRtl ? TextFormatFlags.Right : TextFormatFlags.Left;
                        rect.Offset(1, 1);
                        break;
                    case HorizontalAlignment.Right:
                        flags |= isRtl ? TextFormatFlags.Left : TextFormatFlags.Right;
                        rect.Offset(0, 1);
                        break;
                }
                using var graphics = CreateGraphics();
                if (TextLength > 0)
                {
                    // cover the blank text (password dots are visible) before drawing the placeholder over it
                    graphics.Clear(BackColor);
                }
                DrawText(graphics, _cue, Font, rect, _cueColor, flags);
            }
        }
        #endregion
    }
}
