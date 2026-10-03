using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Math;
using static System.Windows.Forms.TextRenderer;
using static YANF.Control.YANEditPaint;
using static YANF.Control.YANPaint;

namespace YANF.Control
{
    [DefaultBindingProperty(nameof(Value))]
    [DefaultEvent(nameof(ValueChanged))]
    [DefaultProperty(nameof(Value))]
    [ToolboxBitmap(typeof(NumericUpDown))]
    public class YANNb : UserControl
    {
        #region Fields
        private Color _borderColor = MediumSlateBlue;
        private Color _borderFocusColor = LightYellow;
        private string _innerAccessibleName = null;
        private string _innerAccessibleDescription = null;
        private bool _is_UnderlinedStyle = false;
        private bool _is_Focus = false;
        private readonly NumericUpDown _nudNum;
        // BorderSize and BorderRadius (96-dpi pixels), the rounded shape of the control and its border
        private readonly YANBorder _border;
        private const int WM_DPICHANGED_AFTERPARENT = 0x02E3;
        #endregion

        #region Constructors
        public YANNb()
        {
            _nudNum = new NumericUpDown();
            _border = new YANBorder(this, 1, 0);
            SuspendLayout();
            // numeric num
            _nudNum.BackColor = White;
            _nudNum.BorderStyle = BorderStyle.None;
            _nudNum.TextAlign = HorizontalAlignment.Center;
            _nudNum.Dock = DockStyle.Fill;
            _nudNum.Location = new Point(10, 7);
            _nudNum.Size = new Size(180, 18);
            _nudNum.Font = new Font(Font.Name, 11f);
            _nudNum.Enter += Nud_Enter;
            _nudNum.Leave += Nud_Leave;
            _nudNum.KeyDown += Nud_KeyDown;
            _nudNum.KeyPress += Nud_KeyPress;
            _nudNum.ValueChanged += Nud_ValueChanged;
            _nudNum.SizeChanged += Nud_BoundsChanged;
            _nudNum.LocationChanged += Nud_BoundsChanged;
            // user control
            Controls.Add(_nudNum);
            DoubleBuffered = true;
            ResizeRedraw = true;
            ForeColor = DimGray;
            BackColor = White;
            ThousandsSeparator = true;
            // 2.0: no AutoScaleMode.None, the control scales with its form like any other container (Padding, Size, the inner box)
            String = Value.ToString();
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
        public string String;

        [Category("YAN Appearance"), Description("Indicates how the text should be aligned for edit controls.")]
        [DefaultValue(HorizontalAlignment.Center)]
        public HorizontalAlignment TextAlign
        {
            get => _nudNum.TextAlign;
            set
            {
                if (_nudNum.TextAlign != value)
                {
                    _nudNum.TextAlign = value;
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
        [DefaultValue(typeof(Color), "LightYellow")]
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

        [Category("YAN Data"), Description("Indicates the minimum value for the numeric up-down control.")]
        [DefaultValue(typeof(decimal), "0")]
        public decimal Minimum
        {
            get => _nudNum.Minimum;
            set
            {
                // NumericUpDown raises Maximum and constrains Value itself, so Maximum and Minimum can be set in any order
                if (_nudNum.Minimum != value)
                {
                    _nudNum.Minimum = value;
                }
            }
        }

        [Category("YAN Data"), Description("Indicates the maximum value for the numeric up-down control.")]
        [DefaultValue(typeof(decimal), "100")]
        public decimal Maximum
        {
            get => _nudNum.Maximum;
            set
            {
                // NumericUpDown lowers Minimum and constrains Value itself, so Maximum and Minimum can be set in any order
                if (_nudNum.Maximum != value)
                {
                    _nudNum.Maximum = value;
                }
            }
        }

        [Category("YAN Data"), Description("The current value of the numeric up-down control.")]
        [Bindable(true)]
        [DefaultValue(typeof(decimal), "0")]
        public decimal Value
        {
            get => _nudNum.Value;
            set
            {
                _nudNum.Value = Max(Minimum, Min(Maximum, value));
                String = _nudNum.Value.ToString();
            }
        }

        [Category("YAN Data"), Description("Indicates the amount to increment or decrement on each button click.")]
        [DefaultValue(typeof(decimal), "1")]
        public decimal Increment { get => _nudNum.Increment; set => _nudNum.Increment = value; }

        /// <summary>
        /// Gets or sets the width of the border in 96-dpi pixels: it is scaled to the DPI of the control. In <see cref="UnderlinedStyle"/>
        /// the underline is BorderSize / 2 + 1 pixels thick at 96 dpi, as in 1.x (none for 0 when the corners are rounded, as in 1.x). A
        /// negative value is stored as 0.
        /// </summary>
        [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
        [DefaultValue(1)]
        public int BorderSize
        {
            get => _border.Size;
            set
            {
                value = Max(0, value);
                if (_border.Size != value)
                {
                    _border.Size = value;
                    UpdateNudRegion();
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
                    UpdateNudRegion();
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("Indicates the number of decimal places to display.")]
        [DefaultValue(0)]
        public int DecimalPlaces
        {
            get => _nudNum.DecimalPlaces;
            set
            {
                if (_nudNum.DecimalPlaces != value)
                {
                    _nudNum.DecimalPlaces = value;
                    Invalidate();
                }
            }
        }

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

        [Category("YAN Appearance"), Description("Indicates whether the thousands separator will be inserted between every three decimal digits.")]
        [DefaultValue(true)]
        public bool ThousandsSeparator
        {
            get => _nudNum.ThousandsSeparator;
            set
            {
                if (_nudNum.ThousandsSeparator != value)
                {
                    _nudNum.ThousandsSeparator = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the value can be changed only with the up and down buttons (and the arrow keys), not by typing.
        /// </summary>
        [Category("YAN Behavior"), Description("Indicates whether the text can be changed by the use of the up or down buttons only.")]
        [DefaultValue(false)]
        public bool ReadOnly { get => _nudNum.ReadOnly; set => _nudNum.ReadOnly = value; }

        //event
        /// <summary>
        /// Occurs when the value of the <see cref="Value"/> property changes (typed, spun or set by code). The sender is this control
        /// (2.0; 1.x passed the inner NumericUpDown). Raised by <see cref="OnValueChanged(EventArgs)"/>.
        /// </summary>
        [Category("YAN Event"), Description("Occurs when the value of the Value property changes.")]
        public event EventHandler ValueChanged;
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
                _nudNum.BackColor = value;
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
                _nudNum.ForeColor = value;
            }
        }

        [Category("Appearance"), Description("The font used to display text in the control.")]
        public override Font Font
        {
            get => base.Font;
            set
            {
                base.Font = value;
                _nudNum.Font = value;
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
        /// <see cref="BorderFocusColor"/> while the control has the focus; the system frame and highlight colors in high contrast mode.
        /// </summary>
        /// <param name="e">The paint data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // 1.x drew no underline for a BorderSize of 0 on a rounded YANNb (a one-pixel line on a square one, as YANTxt does on both)
            PaintEditBorder(this, e.Graphics, _border, _is_UnderlinedStyle, _is_Focus ? _borderFocusColor : _borderColor, _is_Focus, false);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateNudRegion();
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
        /// Called when the control is first created: also gives the inner numeric up-down the <see cref="System.Windows.Forms.Control.AccessibleName"/> and
        /// <see cref="System.Windows.Forms.Control.AccessibleDescription"/> of this control (unless they were set on it directly), so that screen readers announce them.
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
        /// inner numeric up-down are rebuilt for the new DPI.
        /// </summary>
        /// <param name="deviceDpiOld">The DPI before the change.</param>
        /// <param name="deviceDpiNew">The new DPI.</param>
        protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
        {
            base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
            _border.Reset();
            UpdateNudRegion();
            Invalidate();
        }

        /// <summary>
        /// Lets the clicks on the transparent rounded corners through to the parent, as the rounded region of 1.x did, and fits the
        /// height of the control to its font again once its form has been rescaled for another DPI (per-monitor DPI awareness).
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
                UpdateHCtrl();
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
        // Raises the enter event
        private void Nud_Enter(object sender, EventArgs e)
        {
            _is_Focus = true;
            // AccessibleName has no change event: pick up a value set after the control was created before it is announced
            ForwardAccessibility();
            _nudNum.Select(0, _nudNum.Text.Length);
            Invalidate();
        }

        // Raises the leave event
        private void Nud_Leave(object sender, EventArgs e)
        {
            _is_Focus = false;
            if (string.IsNullOrWhiteSpace(_nudNum.Text))
            {
                _nudNum.Text = _nudNum.Value.ToString();
            }
            Invalidate();
        }

        // Raises the key down event
        private void Nud_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e);
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
            }
        }

        // Raises the key press event
        private void Nud_KeyPress(object sender, KeyPressEventArgs e) => OnKeyPress(e);

        // Raises the value changed event and keeps the String field in sync with the value
        private void Nud_ValueChanged(object sender, EventArgs e)
        {
            String = _nudNum.Value.ToString();
            OnValueChanged(e);
        }

        // Update the region of the numeric up-down when it moves or changes size (a new size or padding of the control)
        private void Nud_BoundsChanged(object sender, EventArgs e) => UpdateNudRegion();
        #endregion

        #region Methods
        /// <summary>
        /// Raises the <see cref="ValueChanged"/> event. Override it to run code before or after the handlers (call the base method).
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnValueChanged(EventArgs e) => ValueChanged?.Invoke(this, e); // 2.0: this control is the sender (1.x: the inner NumericUpDown)

        // Update the region of the numeric up-down when the size, the shape, the padding or the DPI changes (see YANEditPaint.UpdateEditRegion)
        private void UpdateNudRegion()
        {
            if (_nudNum != null && _border != null)
            {
                UpdateEditRegion(this, _nudNum, _border, false);
            }
        }

        // Copy AccessibleName and AccessibleDescription to the inner numeric up-down (the control that screen readers announce), keeping a value set on it directly
        private void ForwardAccessibility()
        {
            if (_nudNum.AccessibleName == _innerAccessibleName)
            {
                _nudNum.AccessibleName = _innerAccessibleName = AccessibleName;
            }
            if (_nudNum.AccessibleDescription == _innerAccessibleDescription)
            {
                _nudNum.AccessibleDescription = _innerAccessibleDescription = AccessibleDescription;
            }
        }

        // Update the height of control when changed font display
        private void UpdateHCtrl()
        {
            _nudNum.MinimumSize = new Size(0, MeasureText("0", Font).Height + 1);
            Height = _nudNum.Height + Padding.Top + Padding.Bottom;
        }
        #endregion
    }
}
