using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.ComponentModel.DesignerSerializationVisibility;
using static System.ComponentModel.EditorBrowsableState;
using static System.Drawing.Color;
using static System.Drawing.ContentAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Math;
using static System.Windows.Forms.AutoCompleteMode;
using static System.Windows.Forms.AutoCompleteSource;
using static System.Windows.Forms.ComboBoxStyle;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.DockStyle;
using static System.Windows.Forms.FlatStyle;

namespace YANF.Control;

[DefaultEvent("OnSelectedIndexChanged")]
public class YANDdl : UserControl
{
    #region Fields
    private ContentAlignment _textAlign = MiddleLeft;
    private Color _backColor = WhiteSmoke;
    private Color _iconColor = MediumSlateBlue;
    private Color _listBackColor = FromArgb(230, 228, 245);
    private Color _listTextColor = DimGray;
    private Color _borderColor = MediumSlateBlue;
    private string _string;
    private int _borderSize = 1;
    private readonly Label _lblText;
    private readonly Button _btnIc;
    private readonly ComboBox _cmbList;
    #endregion

    #region Constructors
    public YANDdl()
    {
        _cmbList = new ComboBox();
        _lblText = new Label();
        _btnIc = new Button();
        SuspendLayout();
        // combobox dropdown list
        _cmbList.BackColor = _listBackColor;
        _cmbList.ForeColor = _listTextColor;
        _cmbList.IntegralHeight = false;
        _cmbList.Font = new Font(Font.Name, 10f);
        _cmbList.KeyUp += Ctrl_KeyUp;
        _cmbList.SelectedIndexChanged += Cmb_SelectedIndexChanged;
        _cmbList.TextChanged += Cmb_TextChanged;
        _cmbList.TextChanged += Cmb_StringChanged;
        // button icon
        _btnIc.Dock = DockStyle.Right;
        _btnIc.FlatStyle = Flat;
        _btnIc.FlatAppearance.BorderSize = 0;
        _btnIc.BackColor = _backColor;
        _btnIc.Cursor = Hand;
        _btnIc.TabStop = false;
        _btnIc.Size = new Size(30, 30);
        _btnIc.Click += Ic_Click;
        _btnIc.Paint += Ic_Paint;
        // label text
        _lblText.Dock = Fill;
        _lblText.AutoSize = false;
        _lblText.BackColor = _backColor;
        _lblText.TextAlign = _textAlign;
        _lblText.Padding = new Padding(8, 0, 0, 0);
        _lblText.Font = new Font(Font.Name, 10f);
        _lblText.Click += Surface_Click;
        _lblText.MouseEnter += Surface_MouseEnter;
        _lblText.MouseLeave += Surface_MouseLeave;
        // user control
        Controls.Add(_lblText); // 2
        Controls.Add(_btnIc); // 1
        Controls.Add(_cmbList); // 0
        ForeColor = DimGray;
        AutoCompleteMode = SuggestAppend;
        AutoCompleteSource = ListItems;
        String = "Select...";
        MinimumSize = new Size(200, 30);
        Size = new Size(200, 35);
        Padding = new Padding(_borderSize);
        Font = new Font(Font.Name, 10f);
        Enter += Ddl_Enter;
        Leave += Ddl_Leave;
        Resize += Ctrl_Resize;
        // base
        base.BackColor = _borderColor;
        ResumeLayout();
        AdjustCmbDimension();
    }
    #endregion

    #region Properties
    [Category("YAN Appearance"), Description("Indicates how the text should be aligned for edit controls.")]
    public ContentAlignment TextAlign
    {
        get => _textAlign;
        set
        {
            _textAlign = value;
            Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("The background color of the component.")]
    public new Color BackColor
    {
        get => _backColor;
        set
        {
            _backColor = value;
            _lblText.BackColor = _backColor;
            _btnIc.BackColor = _backColor;
        }
    }

    [Category("YAN Appearance"), Description("The color of the icon.")]
    public Color IconColor
    {
        get => _iconColor;
        set
        {
            _iconColor = value;
            _btnIc.Invalidate();
        }
    }

    [Category("YAN Appearance"), Description("The background color of the list of the component.")]
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
    public Color ListTextColor
    {
        get => _listTextColor;
        set
        {
            _listTextColor = value;
            _cmbList.ForeColor = _listTextColor;
        }
    }

    [Category("YAN Appearance"), Description("This property specifies the color of the border around the control.")]
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            base.BackColor = _borderColor;
        }
    }

    [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
    public int BorderSize
    {
        get => _borderSize;
        set
        {
            _borderSize = value;
            Padding = new Padding(_borderSize);
            AdjustCmbDimension();
        }
    }

    [Category("YAN Appearance"), Description("The text associated with the control.")]
    public string String { get => _lblText.Text; set => _lblText.Text = value; }

    [Category("YAN Appearance"), Description("Controls the appearance and functionality of the combo box.")]
    public ComboBoxStyle DropDownStyle
    {
        get => _cmbList.DropDownStyle;
        set
        {
            if (_cmbList.DropDownStyle != Simple)
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

    [Category("YAN Appearance"), Description("The autocomplete custom source, which is a custom StringCollection used when the AutoCompleteSource is CustomSource.")]
    [Browsable(true)]
    [DesignerSerializationVisibility(Content)]
    [Editor("System.Windows.Forms.Design.ListControlStringCollectionEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
    [EditorBrowsable(Always)]
    [Localizable(true)]
    public AutoCompleteStringCollection AutoCompleteCustomSource { get => _cmbList.AutoCompleteCustomSource; set => _cmbList.AutoCompleteCustomSource = value; }

    [Category("YAN Data"), Description("The source of complete strings used for automatic completion.")]
    [Browsable(true)]
    [DefaultValue(AutoCompleteSource.None)]
    [EditorBrowsable(Always)]
    public AutoCompleteSource AutoCompleteSource { get => _cmbList.AutoCompleteSource; set => _cmbList.AutoCompleteSource = value; }

    [Category("YAN Data"), Description("Indicates the text completion behavior of the combo box.")]
    [Browsable(true)]
    [DefaultValue(AutoCompleteMode.None)]
    [EditorBrowsable(Always)]
    public AutoCompleteMode AutoCompleteMode { get => _cmbList.AutoCompleteMode; set => _cmbList.AutoCompleteMode = value; }

    [Bindable(true)]
    [Browsable(false)]
    [DesignerSerializationVisibility(Hidden)]
    public object SelectedItem { get => _cmbList.SelectedItem; set => _cmbList.SelectedItem = value; }

    [Browsable(false)]
    [DesignerSerializationVisibility(Hidden)]
    public int SelectedIndex { get => _cmbList.SelectedIndex; set => _cmbList.SelectedIndex = value; }

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
    [Category("YAN Event"), Description("Occurs when the value of the SelectedIndex property changes.")]
    public event EventHandler OnSelectedIndexChanged;

    [Category("YAN Event"), Description("Event raised when the value of the Txt property is changed on Control.")]
    public event EventHandler StringChanged;
    #endregion

    #region Overridden
    // Foreground color
    public override Color ForeColor
    {
        get => base.ForeColor;
        set
        {
            base.ForeColor = value;
            _lblText.ForeColor = value;
        }
    }

    // Font display
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

    // On resize
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        AdjustCmbDimension();
    }

    // On paint
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _lblText.TextAlign = _textAlign;
    }
    #endregion

    #region Events
    // Raises the key up event
    private void Ctrl_KeyUp(object sender, KeyEventArgs e) => OnKeyUp(e);

    // Raises the selected index changed event
    private void Cmb_SelectedIndexChanged(object sender, EventArgs e)
    {
        _lblText.Text = _cmbList.Text;
        if (OnSelectedIndexChanged != null)
        {
            OnSelectedIndexChanged.Invoke(sender, e);
        }
    }

    // Raises the text changed event
    private void Cmb_StringChanged(object sender, EventArgs e)
    {
        if (StringChanged != null)
        {
            StringChanged.Invoke(sender, e);
        }
    }

    // Raises the text changed event
    private void Cmb_TextChanged(object sender, EventArgs e)
    {
        if (_cmbList.DropDownStyle != DropDownList && !string.IsNullOrEmpty(_cmbList.Text))
        {
            _lblText.Text = _cmbList.Text;
        }
    }

    // Raises the mouse enter event
    private void Surface_MouseEnter(object sender, EventArgs e) => OnMouseEnter(e);

    // Raises the mouse leave event
    private void Surface_MouseLeave(object sender, EventArgs e) => OnMouseLeave(e);

    // Raises the click event
    private void Surface_Click(object sender, EventArgs e)
    {
        OnClick(e);
        _cmbList.Select();
        if (_cmbList.DropDownStyle == DropDownList)
        {
            _cmbList.DroppedDown = true;
        }
    }

    // Icon paint event
    private void Ic_Paint(object sender, PaintEventArgs e)
    {
        var wIc = 14;
        var hIc = 6;
        var rectIc = new Rectangle((_btnIc.Width - wIc) / 2, (_btnIc.Height - hIc) / 2, wIc, hIc);
        var graphics = e.Graphics;
        // draw arrow down icon
        using var path = new GraphicsPath();
        using var pen = new Pen(_iconColor, 2);
        graphics.SmoothingMode = AntiAlias;
        path.AddLine(rectIc.X, rectIc.Y, rectIc.X + wIc / 2, rectIc.Bottom);
        path.AddLine(rectIc.X + wIc / 2, rectIc.Bottom, rectIc.Right, rectIc.Y);
        graphics.DrawPath(pen, path);
    }

    // Icon click event
    private void Ic_Click(object sender, EventArgs e)
    {
        _cmbList.Select();
        _cmbList.DroppedDown = true;
    }

    // Raises the enter event
    private void Ddl_Enter(object sender, EventArgs e)
    {
        if (_cmbList.DropDownStyle != DropDownList)
        {
            if (string.IsNullOrEmpty(_string))
            {
                _string = _lblText.Text;
            }
            _lblText.Text = null;
        }
    }

    // Raises the leave event
    private void Ddl_Leave(object sender, EventArgs e)
    {
        if (_cmbList.DropDownStyle != DropDownList && string.IsNullOrEmpty(_lblText.Text))
        {
            _lblText.Text = _string;
        }
    }

    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e)
    {
        var minSize = Width > Height ? Height : Width;
        _borderSize = Min(_borderSize, minSize / 2);
    }
    #endregion

    #region Methods
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
