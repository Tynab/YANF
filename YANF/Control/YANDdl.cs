using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using static System.ComponentModel.DesignerSerializationVisibility;
using static System.ComponentModel.EditorBrowsableState;
using static System.Drawing.Color;
using static System.Drawing.ContentAlignment;
using static System.Math;
using static System.Windows.Forms.AutoCompleteMode;
using static System.Windows.Forms.AutoCompleteSource;
using static System.Windows.Forms.ComboBoxStyle;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.DockStyle;
using static System.Windows.Forms.FlatStyle;
using static YANF.Control.YANPaint;

namespace YANF.Control;

[DefaultBindingProperty(nameof(Text))]
[DefaultEvent(nameof(SelectedIndexChanged))]
[DefaultProperty(nameof(Items))]
[LookupBindingProperties(nameof(DataSource), nameof(DisplayMember), nameof(ValueMember), nameof(SelectedValue))]
[ToolboxBitmap(typeof(ComboBox))]
public partial class YANDdl : UserControl
{
    #region Fields
    private ContentAlignment _textAlign = MiddleLeft;
    private Color _iconColor = MediumSlateBlue;
    private Color _iconFocusColor = HotPink;
    private Color _listBackColor = FromArgb(230, 228, 245);
    private Color _listTextColor = DimGray;
    private Color _borderColor = MediumSlateBlue;
    private Color _borderFocusColor = HotPink;
    private string _string;
    private string _innerAccessibleName = null;
    private string _innerAccessibleDescription = null;
    private bool _is_Focus = false;
    private readonly Label _lblText;
    private readonly Button _btnIc;
    private readonly ComboBox _cmbList;
    // BorderSize (96-dpi pixels) and its width at the DPI of the control (the border fills the band outside the surface, see OnPaintBackground)
    private readonly YANBorder _border;
    // the arrow of the icon: its width, its height and the width of its pen, in 96-dpi pixels
    private const int ARROW_WIDTH = 14;
    private const int ARROW_HEIGHT = 6;
    private const float ARROW_PEN_WIDTH = 2f;
    #endregion

    #region Constructors
    public YANDdl()
    {
        _cmbList = new ComboBox();
        _lblText = new Label();
        _btnIc = new Button();
        _border = new YANBorder(this, 1, 0);
        SuspendLayout();
        // combobox dropdown list
        _cmbList.BackColor = _listBackColor;
        _cmbList.ForeColor = _listTextColor;
        _cmbList.IntegralHeight = false;
        _cmbList.Font = new Font(Font.Name, 10f);
        _cmbList.KeyUp += Ctrl_KeyUp;
        _cmbList.SelectedIndexChanged += Cmb_SelectedIndexChanged;
        _cmbList.SelectedValueChanged += Cmb_SelectedValueChanged;
        _cmbList.TextChanged += Cmb_TextChanged;
        _cmbList.TextChanged += Cmb_StringChanged;
        // button icon
        _btnIc.Dock = DockStyle.Right;
        _btnIc.FlatStyle = Flat;
        _btnIc.FlatAppearance.BorderSize = 0;
        _btnIc.Cursor = Hand;
        _btnIc.TabStop = false;
        _btnIc.Size = new Size(30, 30);
        _btnIc.Paint += Ic_Paint;
        _btnIc.Click += Ic_Click;
        // label text
        _lblText.Dock = Fill;
        _lblText.AutoSize = false;
        _lblText.TextAlign = _textAlign;
        _lblText.Padding = new Padding(8, 0, 0, 0);
        _lblText.Font = new Font(Font.Name, 10f);
        _lblText.MouseEnter += Surface_MouseEnter;
        _lblText.MouseLeave += Surface_MouseLeave;
        _lblText.Click += Surface_Click;
        // user control
        Controls.Add(_lblText); // 2
        Controls.Add(_btnIc); // 1
        Controls.Add(_cmbList); // 0
        // the border is painted along the edge: repaint all of it when the size changes
        ResizeRedraw = true;
        ForeColor = DimGray;
        // 2.0: the surface (also given to the label and the icon button); the border is painted from BorderColor
        BackColor = WhiteSmoke;
        AutoCompleteMode = SuggestAppend;
        AutoCompleteSource = ListItems;
        String = "Select...";
        MinimumSize = new Size(200, 30);
        Size = new Size(200, 35);
        Padding = GetBorderPadding();
        Font = new Font(Font.Name, 10f);
        Enter += Ddl_Enter;
        Leave += Ddl_Leave;
        // the parent is painted behind a transparent surface or border: repaint when it moves or when the parent changes behind it
        FollowParent(this);
        // base
        ResumeLayout();
        AdjustCmbDimension();
    }
    #endregion

    #region Properties
    [Category("YAN Appearance"), Description("Indicates how the text should be aligned for edit controls.")]
    [DefaultValue(ContentAlignment.MiddleLeft)]
    public ContentAlignment TextAlign
    {
        get => _textAlign;
        set
        {
            _textAlign = value;
            _lblText.TextAlign = value;
        }
    }

    /// <summary>
    /// Gets or sets the background color of the control: its surface inside the border, behind the text and the icon.
    /// </summary>
    /// <remarks>
    /// 2.0: this overrides <see cref="System.Windows.Forms.Control.BackColor"/>, so code that colors controls through a Control
    /// reference (a theme loop, for example) colors the surface, and <see cref="System.Windows.Forms.Control.BackColorChanged"/> is
    /// raised. In 1.x it hid Control.BackColor, which held the border color. A transparent or translucent color lets the parent's
    /// own background show through.
    /// </remarks>
    [Category("YAN Appearance"), Description("The background color of the component.")]
    [DefaultValue(typeof(Color), "WhiteSmoke")]
    public override Color BackColor
    {
        get => base.BackColor;
        set
        {
            base.BackColor = value;
            // the label and the icon button fill the surface
            _lblText.BackColor = value;
            _btnIc.BackColor = value;
        }
    }

    [Category("YAN Appearance"), Description("The color of the icon.")]
    [DefaultValue(typeof(Color), "MediumSlateBlue")]
    public Color IconColor
    {
        get => _iconColor;
        set
        {
            _iconColor = value;
            _btnIc.Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the color of the icon while the control has the focus. <see cref="Color.Empty"/> or a transparent color keeps <see cref="IconColor"/>.
    /// </summary>
    /// <remarks>
    /// It only replaces an arrow that can be seen: while <see cref="IconColor"/> is fully transparent (a 1.x designer file that hides
    /// the arrow), the arrow stays hidden when the control has the focus.
    /// </remarks>
    [Category("YAN Appearance"), Description("The color of the icon when the control has the focus.")]
    [DefaultValue(typeof(Color), "HotPink")]
    public Color IconFocusColor
    {
        get => _iconFocusColor;
        set
        {
            if (_iconFocusColor != value)
            {
                _iconFocusColor = value;
                _btnIc.Invalidate();
            }
        }
    }

    [Category("YAN Appearance"), Description("The background color of the list of the component.")]
    [DefaultValue(typeof(Color), "230, 228, 245")]
    public Color ListBackColor
    {
        get => _listBackColor;
        set
        {
            _listBackColor = value;
            _cmbList.BackColor = _listBackColor;
        }
    }

    [Category("YAN Appearance"), Description("The foreground color of this component, which is used to display list.")]
    [DefaultValue(typeof(Color), "DimGray")]
    public Color ListTextColor
    {
        get => _listTextColor;
        set
        {
            _listTextColor = value;
            _cmbList.ForeColor = _listTextColor;
        }
    }

    /// <summary>
    /// Gets or sets the color of the border, painted between the edge of the control and its
    /// <see cref="System.Windows.Forms.Control.Padding"/> as in 1.x (2.0: it no longer changes
    /// <see cref="System.Windows.Forms.Control.BackColor"/>). A transparent or translucent color lets the parent's own background
    /// show through.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the color of the border while the control has the focus. <see cref="Color.Empty"/> or a transparent color keeps <see cref="BorderColor"/>.
    /// </summary>
    /// <remarks>
    /// It only replaces a border that can be seen: while <see cref="BorderColor"/> is fully transparent (a 1.x designer file that uses
    /// <see cref="System.Windows.Forms.Control.Padding"/> as a text indent without a border), the band stays transparent when the
    /// control has the focus, also in high contrast mode.
    /// </remarks>
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

    /// <summary>
    /// Gets or sets the width of the border in 96-dpi pixels: it is scaled to the DPI of the control. Setting it also sets
    /// <see cref="System.Windows.Forms.Control.Padding"/> to the scaled width, so the text and the icon sit inside the border. A
    /// negative value is stored as 0.
    /// </summary>
    [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
    [DefaultValue(1)]
    public int BorderSize
    {
        get => _border.Size;
        set
        {
            _border.Size = Max(0, value);
            Padding = GetBorderPadding();
            AdjustCmbDimension();
            Invalidate();
        }
    }

    /// <summary>
    /// Gets or sets the text shown by the control: the prompt (for example "Select...") while the combo box is blank, otherwise the
    /// selected or typed text (see <see cref="Text"/> for the value). Setting it while the combo box is blank also sets the prompt that
    /// is shown again when the text is cleared.
    /// </summary>
    [Category("YAN Appearance"), Description("The text associated with the control.")]
    [DefaultValue("Select...")]
    [Localizable(true)]
    public string String
    {
        get => _lblText.Text;
        set
        {
            _lblText.Text = value;
            // the label shows the prompt while the combo box is blank: remember it, so that clearing the text shows it again
            if (string.IsNullOrWhiteSpace(_cmbList.Text))
            {
                _string = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the text of the combo box: the text of the selected item, or the typed text when <see cref="DropDownStyle"/> is
    /// DropDown (with DropDownList, setting it selects the item with that text). <see cref="TextChanged"/> is raised when it changes.
    /// </summary>
    /// <remarks>
    /// This is the value of the control and it can be data-bound; it is never the prompt shown by <see cref="String"/>, so it is ""
    /// until an item is selected or text is typed. It is not written by the designer.
    /// </remarks>
    [Category("YAN Data"), Description("The text of the combo box: the selected item or the typed text.")]
    [Bindable(true)]
    [Browsable(false)]
    [DesignerSerializationVisibility(Hidden)]
    [EditorBrowsable(Always)]
    public override string Text { get => _cmbList.Text; set => _cmbList.Text = value; }

    [Category("YAN Appearance"), Description("Controls the appearance and functionality of the combo box.")]
    [DefaultValue(ComboBoxStyle.DropDown)]
    public ComboBoxStyle DropDownStyle
    {
        get => _cmbList.DropDownStyle;
        set
        {
            // Simple (list always visible) does not fit the layout of this control, so it is ignored
            if (value != Simple)
            {
                _cmbList.DropDownStyle = value;
            }
        }
    }

    // Data
    [Category("YAN Data"), Description("The items in the combo box.")]
    [DesignerSerializationVisibility(Content)]
    [Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
    [Localizable(true)]
    [MergableProperty(false)]
    public ComboBox.ObjectCollection Items => _cmbList.Items;

    [Category("YAN Data"), Description("Indicates the list that this control will use to get its items.")]
    [AttributeProvider(typeof(IListSource))]
    [DefaultValue(null)]
    public object DataSource { get => _cmbList.DataSource; set => _cmbList.DataSource = value; }

    [Category("YAN Behavior"), Description("The autocomplete custom source, which is a custom StringCollection used when the AutoCompleteSource is CustomSource.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(Content)]
    [Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
    [EditorBrowsable(Always)]
    [Localizable(true)]
    public AutoCompleteStringCollection AutoCompleteCustomSource { get => _cmbList.AutoCompleteCustomSource; set => _cmbList.AutoCompleteCustomSource = value; }

    [Category("YAN Behavior"), Description("The source of complete strings used for automatic completion.")]
    [Browsable(true)]
    [DefaultValue(AutoCompleteSource.ListItems)]
    [EditorBrowsable(Always)]
    public AutoCompleteSource AutoCompleteSource { get => _cmbList.AutoCompleteSource; set => _cmbList.AutoCompleteSource = value; }

    [Category("YAN Behavior"), Description("Indicates the text completion behavior of the combo box.")]
    [Browsable(true)]
    [DefaultValue(AutoCompleteMode.SuggestAppend)]
    [EditorBrowsable(Always)]
    public AutoCompleteMode AutoCompleteMode { get => _cmbList.AutoCompleteMode; set => _cmbList.AutoCompleteMode = value; }

    [Category("YAN Data"), Description("The selected item of the combo box.")]
    [Bindable(true)]
    [Browsable(false)]
    [DesignerSerializationVisibility(Hidden)]
    public object SelectedItem { get => _cmbList.SelectedItem; set => _cmbList.SelectedItem = value; }

    [Category("YAN Data"), Description("The index of the selected item of the combo box (-1 when nothing is selected).")]
    [Browsable(false)]
    [DesignerSerializationVisibility(Hidden)]
    public int SelectedIndex { get => _cmbList.SelectedIndex; set => _cmbList.SelectedIndex = value; }

    /// <summary>
    /// Gets or sets the value of the <see cref="ValueMember"/> property of the selected item, as for a ComboBox.
    /// </summary>
    /// <remarks>
    /// The getter returns null when no <see cref="DataSource"/> is set (items added through <see cref="Items"/>) or nothing is
    /// selected, and the selected item itself when a DataSource is set and <see cref="ValueMember"/> is empty. The setter selects the
    /// item with that value (nothing is selected when no item has it); it does nothing when no DataSource is set, and throws
    /// <see cref="InvalidOperationException"/> when a DataSource is set and ValueMember is empty, also when a Binding pushes the value.
    /// </remarks>
    [Category("YAN Data"), Description("The value of the member property specified by the ValueMember property for the selected item.")]
    [Bindable(true)]
    [Browsable(false)]
    [DefaultValue(null)]
    [DesignerSerializationVisibility(Hidden)]
    public object SelectedValue { get => _cmbList.SelectedValue; set => _cmbList.SelectedValue = value; }

    [Category("YAN Data"), Description("Indicates the property to display for the items in this control.")]
    [DefaultValue("")]
    [Editor("System.Windows.Forms.Design.DataMemberFieldEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
    [TypeConverter("System.Windows.Forms.Design.DataMemberFieldConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public string DisplayMember { get => _cmbList.DisplayMember; set => _cmbList.DisplayMember = value; }

    [Category("YAN Data"), Description("Indicates the property to use as the actual values for the items in the control.")]
    [DefaultValue("")]
    [Editor("System.Windows.Forms.Design.DataMemberFieldEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
    public string ValueMember { get => _cmbList.ValueMember; set => _cmbList.ValueMember = value; }

    //event
    /// <summary>
    /// Occurs when the value of the <see cref="SelectedIndex"/> property changes. The sender is this control.
    /// </summary>
    [Category("YAN Event"), Description("Occurs when the value of the SelectedIndex property changes.")]
    public event EventHandler SelectedIndexChanged;

    /// <summary>
    /// Occurs when the value of the <see cref="SelectedValue"/> property changes. The sender is this control.
    /// Raised by <see cref="OnSelectedValueChanged(EventArgs)"/>.
    /// </summary>
    [Category("YAN Event"), Description("Occurs when the value of the SelectedValue property changes.")]
    public event EventHandler SelectedValueChanged;

    /// <summary>
    /// Occurs when the value of the <see cref="Text"/> property changes (an item is selected or text is typed). The sender is this
    /// control; focus changes never raise it. Raised by <see cref="System.Windows.Forms.Control.OnTextChanged(EventArgs)"/>.
    /// </summary>
    [Category("YAN Event"), Description("Occurs when the text of the combo box changes.")]
    [Browsable(true)]
    [EditorBrowsable(Always)]
    public new event EventHandler TextChanged { add => base.TextChanged += value; remove => base.TextChanged -= value; }

    /// <summary>
    /// Legacy name of <see cref="SelectedIndexChanged"/>, raised just before it with the inner combo box as the sender (1.0 behaviour).
    /// Use <see cref="SelectedIndexChanged"/>.
    /// </summary>
    [Category("YAN Event"), Description("Occurs when the value of the SelectedIndex property changes (legacy, use SelectedIndexChanged).")]
    [Browsable(false)]
    [EditorBrowsable(Never)]
    public event EventHandler OnSelectedIndexChanged;

    /// <summary>
    /// Legacy event raised with <see cref="TextChanged"/> (when the text of the combo box changes, not when <see cref="String"/>
    /// changes), with the inner combo box as the sender (1.0 behaviour). Use <see cref="TextChanged"/>.
    /// </summary>
    [Category("YAN Event"), Description("Event raised when the text of the combo box changes (legacy, use TextChanged).")]
    [Browsable(false)]
    [EditorBrowsable(Never)]
    public event EventHandler StringChanged;
    #endregion

    #region Overridden
    [Category("Appearance"), Description("The foreground color of this component, which is used to display text.")]
    [DefaultValue(typeof(Color), "DimGray")]
    public override Color ForeColor
    {
        get => base.ForeColor;
        set
        {
            base.ForeColor = value;
            _lblText.ForeColor = value;
        }
    }

    [Category("Appearance"), Description("The font used to display text in the control.")]
    public override Font Font
    {
        get => base.Font;
        set
        {
            base.Font = value;
            _lblText.Font = value;
            _cmbList.Font = value;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        AdjustCmbDimension();
    }

    /// <summary>
    /// Raises the Paint event (the text alignment is given to the label by <see cref="TextAlign"/>; the border and the surface are
    /// painted by <see cref="OnPaintBackground(PaintEventArgs)"/>).
    /// </summary>
    /// <param name="e">The paint data.</param>
    protected override void OnPaint(PaintEventArgs e) => base.OnPaint(e);

    /// <summary>
    /// Paints the background: the surface in <see cref="BackColor"/> (and the BackgroundImage) inside the border, and the border in
    /// <see cref="BorderColor"/>, or in <see cref="BorderFocusColor"/> while the control has the focus (over BorderColor when it is
    /// translucent; never over a fully transparent BorderColor, which stays transparent); in high contrast mode the border is painted
    /// in the system frame and highlight colors (the surface, the text and the arrow keep theirs). As in 1.x, the border fills the
    /// whole band between the edge of the control and its <see cref="System.Windows.Forms.Control.Padding"/> (at least
    /// <see cref="BorderSize"/> wide), so a designer file that sets a wider Padding keeps its look. A transparent or translucent
    /// surface or border shows the parent's own background (a gradient, an image, what its Paint handlers draw); as for a transparent
    /// BackColor in WinForms, sibling controls that overlap this control are not painted behind it.
    /// </summary>
    /// <param name="e">The paint data.</param>
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        var backColor = BackColor;
        var client = ClientRectangle;
        var rectSurface = GetSurfaceRectangle();
        var isBand = rectSurface != client;
        // the focus color replaces a border that can be seen: a transparent one (1.x designer files use the Padding as a text indent
        // without a border) stays transparent
        var isHighlight = _is_Focus && _borderFocusColor.A > 0 && _borderColor.A > 0;
        var isBorderColor = !isHighlight || _borderFocusColor.A < 255;
        var isOpaqueBorder = !isBand || (isHighlight && _borderFocusColor.A == 255) || (isBorderColor && _borderColor.A == 255);
        // what a transparent or translucent surface or border lets through; an opaque control hides it (and the parent's Paint handlers)
        if ((backColor.A < 255 && !rectSurface.IsEmpty) || !isOpaqueBorder)
        {
            PaintParent(this, e);
        }
        // the surface inside the border (with the BackgroundImage, if any, as the default background paints it)
        if (rectSurface.IsEmpty)
        {
            // nothing is left inside the border: it covers the whole control
        }
        else if (BackgroundImage != null)
        {
            var state = graphics.Save();
            try
            {
                graphics.IntersectClip(rectSurface);
                base.OnPaintBackground(e);
            }
            finally
            {
                graphics.Restore(state);
            }
        }
        else if (backColor.A > 0)
        {
            using var brushBack = new SolidBrush(backColor);
            graphics.FillRectangle(brushBack, rectSurface);
        }
        // the border: the band around the surface, on whole pixels
        if (!isBand)
        {
            return;
        }
        using var band = new Region(client);
        band.Exclude(rectSurface);
        // a transparent border stays transparent in high contrast mode too (the band can be a wide Padding, not a frame)
        if (isBorderColor && _borderColor.A > 0)
        {
            FillBand(graphics, band, Contrast(_borderColor, SystemColors.WindowFrame));
        }
        if (isHighlight)
        {
            FillBand(graphics, band, Contrast(_borderFocusColor, SystemColors.Highlight));
        }
    }

    /// <summary>
    /// Called when the control is first created: also gives the inner combo box the <see cref="System.Windows.Forms.Control.AccessibleName"/> and
    /// <see cref="System.Windows.Forms.Control.AccessibleDescription"/> of this control (unless they were set on it directly), so that screen readers announce them.
    /// </summary>
    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        ForwardAccessibility();
    }

    /// <summary>
    /// Called when the DPI of the control changes (per-monitor DPI awareness): the border and the arrow of the icon are painted for
    /// the new DPI (the layout, Padding included, is rescaled by the form).
    /// </summary>
    /// <param name="deviceDpiOld">The DPI before the change.</param>
    /// <param name="deviceDpiNew">The new DPI.</param>
    protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
    {
        base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
        _border.Reset();
        Invalidate();
        _btnIc.Invalidate();
    }

    /// <summary>
    /// Releases the resources used by the control; the cached border shape is released after its window is destroyed.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // after the window is gone, so that no late paint builds the border again
        if (disposing)
        {
            _border.Dispose();
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Raises the <see cref="SelectedValueChanged"/> event with this control as the sender.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnSelectedValueChanged(EventArgs e) => SelectedValueChanged?.Invoke(this, e);

    // Show the text of the combo box in the label; blank text shows the prompt, or nothing while the user edits it (DropDown style)
    private void ShowComboText()
    {
        var text = _cmbList.Text;
        _lblText.Text = !string.IsNullOrWhiteSpace(text) ? text : (_is_Focus && _cmbList.DropDownStyle != DropDownList ? null : _string);
    }

    // Get the surface: the client rectangle inside the border and the padding (the label and the icon button cover it; 1.x showed its
    // background, the border color, around them), empty when nothing is left inside
    private Rectangle GetSurfaceRectangle()
    {
        var size = _border.DeviceSize;
        var padding = Padding;
        var client = ClientRectangle;
        var rect = Rectangle.FromLTRB(Max(size, padding.Left), Max(size, padding.Top), client.Right - Max(size, padding.Right), client.Bottom - Max(size, padding.Bottom));
        return rect.Width > 0 && rect.Height > 0 ? rect : Rectangle.Empty;
    }

    // Fill the band of the border with a color (nothing for a fully transparent color)
    private static void FillBand(Graphics graphics, Region band, Color color)
    {
        if (color.A > 0)
        {
            using var brush = new SolidBrush(color);
            graphics.FillRegion(brush, band);
        }
    }

    // Get the padding that keeps the label and the icon button inside the border: its width at the DPI of the control (a layout
    // value in device pixels, which the form's AutoScale rescales like the Padding written by the designer)
    private Padding GetBorderPadding() => new(LogicalToDevice(this, _border.Size));

    // Get the painted color of the icon: IconFocusColor while the control has the focus (unless it is empty or transparent, or the
    // arrow is hidden by a transparent IconColor)
    private Color GetIconColor() => _is_Focus && _iconFocusColor.A > 0 && _iconColor.A > 0 ? _iconFocusColor : _iconColor;

    // Track the focus and repaint the border and the icon, which are highlighted while the control has the focus
    private void SetFocusHighlight(bool isFocus)
    {
        _is_Focus = isFocus;
        Invalidate();
        _btnIc.Invalidate();
    }

    // Copy AccessibleName and AccessibleDescription to the inner combo box (the control that screen readers announce), keeping a value set on it directly
    private void ForwardAccessibility()
    {
        if (_cmbList.AccessibleName == _innerAccessibleName)
        {
            _cmbList.AccessibleName = _innerAccessibleName = AccessibleName;
        }
        if (_cmbList.AccessibleDescription == _innerAccessibleDescription)
        {
            _cmbList.AccessibleDescription = _innerAccessibleDescription = AccessibleDescription;
        }
    }

    // Adjust combo box dimension
    private void AdjustCmbDimension()
    {
        _cmbList.Width = _lblText.Width;
        _cmbList.Location = new Point()
        {
            X = Width - Padding.Right - _cmbList.Width,
            Y = _lblText.Bottom - _cmbList.Height
        };
    }
    #endregion
}