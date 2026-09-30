using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using static System.ComponentModel.DesignerSerializationVisibility;
using static System.ComponentModel.EditorBrowsableState;
using static System.Drawing.Color;
using static System.Math;
using static System.Windows.Forms.TextRenderer;
using static YANF.Control.YANEditPaint;
using static YANF.Control.YANPaint;

namespace YANF.Control
{
    [DefaultBindingProperty(nameof(Text))]
    [DefaultEvent(nameof(TextChanged))]
    [DefaultProperty(nameof(Text))]
    [ToolboxBitmap(typeof(TextBox))]
    public class YANTxt : UserControl
    {
        #region Fields
        private Color _borderColor = MediumSlateBlue;
        private Color _borderFocusColor = HotPink;
        private Color _placeholderColor = DarkGray;
        private string _placeholderText = null;
        private string _innerAccessibleName = null;
        private string _innerAccessibleDescription = null;
        private bool _is_UnderlinedStyle = false;
        private bool _is_Focus = false;
        private bool _is_Painted = false;
        private readonly CueTextBox _txtText;
        // BorderSize and BorderRadius (96-dpi pixels), the rounded shape of the control and its border
        private readonly YANBorder _border;
        private const int WM_DPICHANGED_AFTERPARENT = 0x02E3;
        #endregion

        #region Constructors
        public YANTxt()
        {
            _txtText = new CueTextBox();
            _border = new YANBorder(this, 2, 0);
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
            _txtText.SizeChanged += Txt_BoundsChanged;
            _txtText.LocationChanged += Txt_BoundsChanged;
            // user control
            Controls.Add(_txtText);
            DoubleBuffered = true;
            ResizeRedraw = true;
            ForeColor = DimGray;
            BackColor = White;
            // 2.0: no AutoScaleMode.None, the control scales with its form like any other container (Padding, Size, the inner box)
            Size = new Size(200, 30);
            Padding = new Padding(10, 7, 10, 7);
            Font = new Font(Font.Name, 11f);
            // the parent is painted behind the rounded corners: repaint when it moves or when the parent changes behind it
            FollowParent(this);
            // base
            ResumeLayout();
        }
        #endregion

        #region Properties
        [Category("YAN Appearance"), Description("Indicates how the text should be aligned for edit controls.")]
        [DefaultValue(HorizontalAlignment.Center)]
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
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
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

        [Category("YAN Appearance"), Description("This property specifies the color of the border around the control when the control has the focus.")]
        [DefaultValue(typeof(Color), "HotPink")]
        public Color BorderFocusColor
        {
            get => _borderFocusColor;
            set
            {
                if (_borderFocusColor != value)
                {
                    _borderFocusColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The color of the placeholder text.")]
        [DefaultValue(typeof(Color), "DarkGray")]
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

        /// <summary>
        /// Gets or sets the text of the control (the text typed by the user).
        /// </summary>
        /// <remarks>
        /// This is the main, data-bindable property of the control; <see cref="TextChanged"/> is raised with this control as
        /// the sender when it changes. The placeholder is painted over an empty box and is never part of the text, so an empty
        /// box returns "" (see <see cref="String"/> for the legacy value that is null while the placeholder shows).
        /// </remarks>
        [Category("YAN Appearance"), Description("The text associated with the control.")]
        [Bindable(true)]
        [Browsable(true)]
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        [EditorBrowsable(Always)]
        [Localizable(true)]
        public override string Text
        {
            get => _txtText.Text;
            set
            {
                // a control dropped in the designer starts empty, like TextBox, instead of showing its site name
                if (!IsSiteNameFromDesigner(value))
                {
                    _txtText.Text = value;
                }
            }
        }

        /// <summary>
        /// Legacy alias of <see cref="Text"/> (1.0): null while the box is unfocused, empty (or white space only) and a
        /// placeholder is set; setting null or white space clears the box when a placeholder is set. Use <see cref="Text"/>.
        /// </summary>
        [Category("YAN Appearance"), Description("The text associated with the control (legacy alias of Text).")]
        [Browsable(false)]
        [DesignerSerializationVisibility(Hidden)]
        [EditorBrowsable(Never)]
        public string String
        {
            // null while the box is unfocused, empty (or whitespace only) and a placeholder is set; the raw text while focused, as in 1.0.2
            get => HasPlaceholder() && !_is_Focus && string.IsNullOrWhiteSpace(_txtText.Text) ? null : _txtText.Text;
            // null or whitespace clears the box when a placeholder is set, as in 1.0.2
            set => _txtText.Text = HasPlaceholder() && string.IsNullOrWhiteSpace(value) ? null : value;
        }

        [Category("YAN Appearance"), Description("The text that is displayed when the control has no text and does not have the focus.")]
        [DefaultValue(null)]
        [Localizable(true)]
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

        /// <summary>
        /// Gets or sets the width of the border in 96-dpi pixels: it is scaled to the DPI of the control. In <see cref="UnderlinedStyle"/>
        /// the underline is BorderSize / 2 + 1 pixels thick at 96 dpi, as in 1.x. A negative value is stored as 0.
        /// </summary>
        [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
        [DefaultValue(2)]
        public int BorderSize
        {
            get => _border.Size;
            set
            {
                value = Max(0, value);
                if (_border.Size != value)
                {
                    _border.Size = value;
                    UpdateTextRegion();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets the radius of the rounded corners in 96-dpi pixels (scaled to the DPI of the control; at most half the
        /// height or width when painted). The corners are painted anti-aliased over the parent's own background. A negative value
        /// is stored as 0.
        /// </summary>
        [Category("YAN Appearance"), Description("This property allows you to add rounded corners to the control.")]
        [DefaultValue(0)]
        public int BorderRadius
        {
            get => _border.Radius;
            set
            {
                value = Max(0, value);
                if (_border.Radius != value)
                {
                    _border.Radius = value;
                    UpdateTextRegion();
                    Invalidate();
                }
            }
        }

        [Category("YAN Behavior"), Description("Specifies the maximum number of characters that can be entered into the edit control.")]
        [DefaultValue(32767)]
        public int MaxLength { get => _txtText.MaxLength; set => _txtText.MaxLength = value; }

        [Category("YAN Appearance"), Description("When this property is true, only a line under the control is drawn instead of the whole border.")]
        [DefaultValue(false)]
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

        /// <summary>
        /// Legacy name of <see cref="UseSystemPasswordChar"/> (a bool, unlike TextBox.PasswordChar). Use <see cref="UseSystemPasswordChar"/>.
        /// </summary>
        [Category("YAN Behavior"), Description("Indicates if the text in the edit control should appear as the default password character (legacy name of UseSystemPasswordChar).")]
        [Browsable(false)]
        [DesignerSerializationVisibility(Hidden)]
        [EditorBrowsable(Never)]
        public bool PasswordChar { get => _txtText.UseSystemPasswordChar; set => _txtText.UseSystemPasswordChar = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the text is masked with the system password character (the legacy
        /// <see cref="PasswordChar"/> property reads and writes the same value).
        /// </summary>
        [Category("YAN Behavior"), Description("Indicates if the text in the edit control should appear as the default password character.")]
        [DefaultValue(false)]
        public bool UseSystemPasswordChar { get => _txtText.UseSystemPasswordChar; set => _txtText.UseSystemPasswordChar = value; }

        /// <summary>
        /// Gets or sets a value indicating whether the text is read-only; read-only text can still be selected and copied.
        /// </summary>
        [Category("YAN Behavior"), Description("Controls whether the text in the edit control can be changed or not.")]
        [DefaultValue(false)]
        public bool ReadOnly { get => _txtText.ReadOnly; set => _txtText.ReadOnly = value; }

        /// <summary>
        /// Gets or sets a value indicating whether ENTER types a new line in a multiline box instead of activating the default
        /// button of the form. When true, ENTER is not suppressed in a single-line box either, so its KeyPress event is raised.
        /// </summary>
        /// <remarks>
        /// A single-line box (when the form has no AcceptButton) then passes ENTER on to the Windows edit control, which answers
        /// with the system beep: handle ENTER in <see cref="System.Windows.Forms.Control.KeyPress"/> and set
        /// <see cref="KeyPressEventArgs.Handled"/> to true to avoid it (the event data is shared with the inner text box).
        /// </remarks>
        [Category("YAN Behavior"), Description("Indicates if return characters are accepted as input for multiline edit controls.")]
        [DefaultValue(false)]
        public bool AcceptsReturn { get => _txtText.AcceptsReturn; set => _txtText.AcceptsReturn = value; }

        [Category("YAN Behavior"), Description("Control whether the text of the edit control can span more than one line.")]
        [DefaultValue(false)]
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
        /// <summary>
        /// Occurs when the value of the <see cref="Text"/> property changes (typed by the user or set by code). The sender is this
        /// control; focus changes never raise it. Raised by <see cref="System.Windows.Forms.Control.OnTextChanged(EventArgs)"/>.
        /// </summary>
        [Category("YAN Event"), Description("Occurs when the value of the Text property changes.")]
        [Browsable(true)]
        [EditorBrowsable(Always)]
        public new event EventHandler TextChanged { add => base.TextChanged += value; remove => base.TextChanged -= value; }

        /// <summary>
        /// Legacy event raised with <see cref="TextChanged"/>, with the inner text box as the sender (1.0 behaviour). Use <see cref="TextChanged"/>.
        /// </summary>
        [Category("YAN Event"), Description("Event raised when the value of the String property is changed on Control (legacy, use TextChanged).")]
        [Browsable(false)]
        [EditorBrowsable(Never)]
        public event EventHandler StringChanged;
        #endregion

        #region Overridden
        [Category("Appearance"), Description("The background color of the component.")]
        [DefaultValue(typeof(Color), "White")]
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                base.BackColor = value;
                _txtText.BackColor = value;
            }
        }

        [Category("Appearance"), Description("The foreground color of this component, which is used to display text.")]
        [DefaultValue(typeof(Color), "DimGray")]
        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                base.ForeColor = value;
                _txtText.ForeColor = value;
            }
        }

        [Category("Appearance"), Description("The font used to display text in the control.")]
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

        /// <summary>
        /// Paints the background: the surface in <see cref="BackColor"/> (and the BackgroundImage), with anti-aliased rounded corners
        /// through which the parent's own background shows (a gradient, an image, what its Paint handlers draw; 1.x painted a ring of
        /// Parent.BackColor there). As for a transparent BackColor in WinForms, sibling controls that overlap this control are not
        /// painted behind it.
        /// </summary>
        /// <param name="e">The paint data.</param>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            PaintEditSurface(this, e, _border, base.OnPaintBackground);
        }

        /// <summary>
        /// Paints the border (or the underline in <see cref="UnderlinedStyle"/>): <see cref="BorderColor"/>, or
        /// <see cref="BorderFocusColor"/> while the box has the focus; the system frame and highlight colors in high contrast mode.
        /// </summary>
        /// <param name="e">The paint data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            _is_Painted = true;
            PaintEditBorder(this, e.Graphics, _border, _is_UnderlinedStyle, _is_Focus ? _borderFocusColor : _borderColor, _is_Focus, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateTextRegion();
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

        /// <summary>
        /// Called when the control is first created: also gives the inner text box the <see cref="System.Windows.Forms.Control.AccessibleName"/> and
        /// <see cref="System.Windows.Forms.Control.AccessibleDescription"/> of this control (unless they were set on it directly), so that screen readers
        /// announce them. Later changes are picked up when the box is entered and whenever an accessibility client asks the box for its accessible object.
        /// </summary>
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ForwardAccessibility();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            // the parent is painted behind the rounded corners
            Invalidate();
        }

        /// <summary>
        /// Called when the DPI of the control changes (per-monitor DPI awareness): the rounded shape, the border and the region of the
        /// inner text box are rebuilt for the new DPI.
        /// </summary>
        /// <param name="deviceDpiOld">The DPI before the change.</param>
        /// <param name="deviceDpiNew">The new DPI.</param>
        protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
        {
            base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
            _border.Reset();
            UpdateTextRegion();
            Invalidate();
        }

        /// <summary>
        /// Lets the clicks on the transparent rounded corners through to the parent, as the rounded region of 1.x did, and fits the
        /// height of a single-line box to its font again once its form has been rescaled for another DPI (per-monitor DPI awareness),
        /// without recreating the window of the inner text box (its text, selection, undo history and scroll position are kept).
        /// </summary>
        /// <param name="m">The message.</param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (_border.HitTest(ref m))
            {
                return;
            }
            if (m.Msg == WM_DPICHANGED_AFTERPARENT && !IsDisposed)
            {
                // the box may have the focus and an edited text: keep the window of the text box
                UpdateHCtrl(false);
            }
        }

        /// <summary>
        /// Releases the resources used by the control; the cached border shapes are released after its window is destroyed.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // after the window is gone, so that no late message builds the shape again
            if (disposing)
            {
                _border.Dispose();
            }
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
            // AccessibleName has no change event: pick up a value set after the control was created before it is announced
            ForwardAccessibility();
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
            if (e.KeyCode == Keys.Enter && !_txtText.Multiline && !_txtText.AcceptsReturn)
            {
                e.SuppressKeyPress = true;
            }
        }

        // Raises the key up event
        private void Txt_KeyUp(object sender, KeyEventArgs e) => OnKeyUp(e);

        // Raises the text changed event (sender = this) and the legacy string changed event (sender = the inner text box, as in 1.0)
        private void Txt_TextChanged(object sender, EventArgs e)
        {
            OnTextChanged(e);
            StringChanged?.Invoke(sender, e);
        }

        // Update the region of the text box when it moves or changes size (a new size or padding of the control)
        private void Txt_BoundsChanged(object sender, EventArgs e) => UpdateTextRegion();
        #endregion

        #region Methods
        // Check whether a placeholder is set
        private bool HasPlaceholder() => !string.IsNullOrWhiteSpace(_placeholderText);

        // Check whether the designer is writing the site name into Text while it initializes a newly dropped control: ControlDesigner
        // does it for every browsable Text (TextBox's own designer clears it again). Designer files being loaded are never ignored
        private bool IsSiteNameFromDesigner(string value)
            => !_is_Painted && _txtText.TextLength == 0 && Site is { DesignMode: true } site && value == site.Name
            && site.GetService(typeof(IDesignerHost)) is not IDesignerHost { Loading: true };

        // Copy AccessibleName and AccessibleDescription to the inner text box (the control that screen readers announce), keeping a value set on it directly
        private void ForwardAccessibility()
        {
            if (_txtText.AccessibleName == _innerAccessibleName)
            {
                _txtText.AccessibleName = _innerAccessibleName = AccessibleName;
            }
            if (_txtText.AccessibleDescription == _innerAccessibleDescription)
            {
                _txtText.AccessibleDescription = _innerAccessibleDescription = AccessibleDescription;
            }
        }

        // Update the region of the inner text box when the size, the shape, the padding or the DPI changes (see UpdateEditRegion)
        private void UpdateTextRegion()
        {
            if (_txtText != null && _border != null)
            {
                UpdateEditRegion(this, _txtText, _border, _txtText.Multiline);
            }
        }

        // Update the height of control when changed font display (as in 1.x: the text box is made multiline for a moment, which gives
        // it a new window handle each time, so this is only done before the user types: when the form loads, and in the designer)
        private void UpdateHCtrl() => UpdateHCtrl(true);

        // Update the height of control when changed font display; without the multiline switch the text box keeps its window (its undo
        // history, scroll position and caret): a single-line text box takes its preferred height, raised to its minimum size
        private void UpdateHCtrl(bool isSwitchingMultiline)
        {
            if (!_txtText.Multiline)
            {
                var minimumSize = new Size(0, MeasureText("Text", Font).Height + 1);
                if (isSwitchingMultiline)
                {
                    _txtText.Multiline = true;
                    _txtText.MinimumSize = minimumSize;
                    _txtText.Multiline = false;
                }
                else
                {
                    _txtText.MinimumSize = minimumSize;
                }
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
            private const int WM_GETOBJECT = 0x003D;
            private string _cue;
            private Color _cueColor;

            // The placeholder text
            [DesignerSerializationVisibility(Hidden)]
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
            [DesignerSerializationVisibility(Hidden)]
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
                // a screen reader asks for the accessible object of the box: give it the current accessible name of the owner first (the
                // accessible object of TextBoxBase is kept, with its UIA support, so the name is copied instead of being read from the owner)
                if (m.Msg == WM_GETOBJECT)
                {
                    (Parent as YANTxt)?.ForwardAccessibility();
                }
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
