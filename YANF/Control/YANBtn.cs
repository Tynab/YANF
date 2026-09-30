using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.DashStyle;
using static System.Math;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.Cursors;
using static YANF.Control.YANPaint;
using static YANF.Script.YANShape;

namespace YANF.Control;

/// <summary>
/// A button with rounded corners and a border. The base class paints the button (its <see cref="ButtonBase.FlatStyle"/>, the
/// colors of its mouse states, its image and text); around the rounded shape the parent's own pixels show (2.0: a gradient, an
/// image, what its Paint handlers draw), with an anti-aliased edge. 1.x cut the button with a region, so whatever lay behind the
/// corners showed through them, sibling controls included, and hid the jagged edge with a ring of Parent.BackColor. 2.0 paints the
/// parent there instead and, as for a transparent BackColor, not the sibling controls that overlap the button: a layout where the
/// button overlaps another control shows the parent in its corners. Clicks on the corners go to the parent, as with the 1.x
/// region. A <see cref="System.Windows.Forms.FlatStyle.System"/> button is painted by Windows: its corners are still cut with a
/// region.
/// </summary>
// IButtonControl is re-implemented so that Form.AcceptButton and Form.CancelButton call the PerformClick below
[ToolboxBitmap(typeof(Button))]
public partial class YANBtn : Button, IButtonControl
{
    #region Fields
    private const int WM_NCHITTEST = 0x0084;
    private Color _borderColor = PaleVioletRed;
    private int _borderSize = 0;
    private int _borderRadius = 20;
    private bool _is_PaintingBase = false;
    private bool _is_SelectableLent = false;
    // the region is the library's (a FlatStyle.System button), not one the application set
    private bool _is_RegionSet = false;
    // the rounded client area (the 1.x region): clicks outside it go to the parent; its radius is 0 when the corners are square
    private readonly YANBorder _hitShape;
    // the painted outlines, built for _shapeSize at _shapeDpi with the current BorderSize and BorderRadius (see UpdateShape)
    private GraphicsPath _pathSurface;
    private GraphicsPath _pathBorder;
    private bool _is_ShapeBuilt = false;
    private bool _is_Rounded = false;
    private int _deviceBorderSize;
    private int _deviceRadius;
    // the ring between the edge of the control and the rounded surface, in whole device pixels
    private int _deviceRing;
    private Size _shapeSize;
    private int _shapeDpi;
    // in 96-dpi pixels: the corners are rounded above this radius, and the focus cue lies this far inside the border
    private const int MIN_ROUND_RADIUS = 2;
    private const int FOCUS_CUE_GAP = 3;
    // without a border, 1.x smoothed the rounded edge with a ring of one 96-dpi pixel inside its region
    private const int EDGE_RING = 1;
    #endregion

    #region Constructors
    public YANBtn()
    {
        _hitShape = new YANBorder(this, 0, 0);
        OptionDisplay();
        // the designer default of FlatAppearance.BorderSize is the value the constructor sets
        TypeDescriptor.AddProvider(new FlatAppearanceDescriptionProvider(TypeDescriptor.GetProvider(FlatAppearance)), FlatAppearance);
        // the parent is painted around the rounded shape: repaint when the button moves or when the parent changes behind it
        FollowParent(this);
    }
    #endregion

    #region Properties
    [Category("YAN Appearance"), Description("This property specifies the color of the border around the control.")]
    [DefaultValue(typeof(Color), "PaleVioletRed")]
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
    /// Gets or sets the width of the border in 96-dpi pixels: it is scaled with the DPI of the control (2.0), and clamped to half
    /// the smaller side when painted.
    /// </summary>
    [Category("YAN Appearance"), Description("This property specifies the size, in pixels, of the border around the control.")]
    [DefaultValue(0)]
    public int BorderSize
    {
        get => _borderSize;
        set
        {
            value = Max(0, value);
            if (_borderSize != value)
            {
                _borderSize = value;
                ResetShape();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the corner radius in 96-dpi pixels: it is scaled with the DPI of the control (2.0), and clamped to half the
    /// smaller side when painted. The corners are square up to a radius of 2 pixels.
    /// </summary>
    [Category("YAN Appearance"), Description("This property allows you to add rounded corners to the control.")]
    [DefaultValue(20)]
    public int BorderRadius
    {
        get => _borderRadius;
        set
        {
            value = Max(0, value);
            if (_borderRadius != value)
            {
                _borderRadius = value;
                ResetShape();
                UpdateRegion();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the button can receive the focus (it wraps <see cref="ControlStyles.Selectable"/>).
    /// When true, the default since 2.0, the button behaves like a standard <see cref="Button"/>: a click moves the focus to it (so
    /// the previously focused control raises Leave and Validating first, and a cancelled validation cancels the click), TAB reaches
    /// it when <see cref="System.Windows.Forms.Control.TabStop"/> is also true, SPACE and ENTER click it while it has the focus,
    /// and a focus cue is drawn inside its shape. Set it to false for the 1.x behaviour: the button never takes the focus.
    /// <see cref="PerformClick"/>, <see cref="Form.AcceptButton"/>, <see cref="Form.CancelButton"/> and mnemonics work either way.
    /// </summary>
    /// <remarks>
    /// 1.x defaulted to false, and its designer did not write that value: a button of a 1.x designer file, or one created in code,
    /// is focusable in 2.0 unless <c>Focusable = false</c> is set. Designer files written by YANF 1.0 set <c>TabStop = false</c>, so
    /// TAB still skips such a button until TabStop is set to true. A button of a designer file written by YANF 1.1 (which does not
    /// write <c>TabStop = true</c>) or created in code is a tab stop: TAB reaches it, and when it comes first in the tab order it
    /// takes the focus when the form opens (instead of, say, the first text box). Set <c>Focusable = false</c> to keep the 1.x
    /// behaviour, or <c>TabStop = false</c> to keep only TAB and the first focus away from it. While this property is false, a
    /// <see cref="Button.PerformClick"/> call that is not bound to <see cref="YANBtn"/> (see the remarks of
    /// <see cref="PerformClick"/>) does nothing.
    /// </remarks>
    [Category("YAN Behavior"), Description("Indicates whether the button can receive the focus: a click then moves the focus to it, TAB reaches it (when TabStop is true) and SPACE or ENTER click it.")]
    [DefaultValue(true)]
    public bool Focusable
    {
        get => GetStyle(Selectable) && !_is_SelectableLent;
        set
        {
            // a Click handler that sets the value ends the loan made by PerformClick
            _is_SelectableLent = false;
            if (GetStyle(Selectable) != value)
            {
                SetStyle(Selectable, value);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets the style of the button, as <see cref="ButtonBase.FlatStyle"/>. Redeclared (2.0) for its designer default:
    /// the constructor sets <see cref="System.Windows.Forms.FlatStyle.Flat"/>, so the designer now writes every other style,
    /// <see cref="System.Windows.Forms.FlatStyle.Standard"/> included (1.x did not write Standard, which the constructor then replaced). Flat, Popup
    /// and Standard are painted in the rounded shape; System is painted by Windows and rounded with a region.
    /// </summary>
    [Category("Appearance"), Description("Determines how the button is drawn. The default is Flat; System buttons are drawn by Windows and rounded with a region.")]
    [DefaultValue(FlatStyle.Flat)]
    [Localizable(true)]
    public new FlatStyle FlatStyle
    {
        get => base.FlatStyle;
        set
        {
            base.FlatStyle = value;
            UpdateRegion();
        }
    }
    #endregion

    #region Overridden
    protected override Cursor DefaultCursor => Hand;

    /// <summary>
    /// Gets a value indicating whether the control should display focus cues. False while the base class paints: the flat
    /// style's rectangular focus cue would cross the rounded corners, so <see cref="OnPaint"/> draws one inside the shape.
    /// </summary>
    protected override bool ShowFocusCues => !_is_PaintingBase && base.ShowFocusCues;

    protected override void OnPaint(PaintEventArgs e)
    {
        // the base paints the button (its face, image and text in the colors of its state) without its focus rectangle (see
        // ShowFocusCues)
        _is_PaintingBase = true;
        try
        {
            base.OnPaint(e);
        }
        finally
        {
            _is_PaintingBase = false;
        }
        var graphics = e.Graphics;
        UpdateShape();
        // around the rounded surface: the parent's own pixels, anti-aliased along the edge
        if (_is_Rounded)
        {
            YANCirPic.PaintParentOutside(this, e, _pathSurface);
        }
        // draw border (in the system frame color in high contrast mode)
        FillPath(graphics, Contrast(_borderColor, SystemColors.WindowFrame), _pathBorder);
        // draw the keyboard focus cue (Windows hides it until the keyboard is used)
        if (Focused && ShowFocusCues)
        {
            DrawFocusCue(graphics);
        }
    }

    // Button.ProcessMnemonic calls the inherited PerformClick, which does nothing while the button cannot be selected
    protected override bool ProcessMnemonic(char charCode)
    {
        if (!GetStyle(Selectable) && UseMnemonic && Enabled && Visible && IsMnemonic(charCode, Text))
        {
            PerformClick();
            return true;
        }
        return base.ProcessMnemonic(charCode);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRegion();
    }

    protected override void OnParentBackColorChanged(EventArgs e)
    {
        base.OnParentBackColorChanged(e);
        Invalidate();
    }

    // The clicks on the corners go to the parent (HTTRANSPARENT), as with the 1.x region
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_NCHITTEST)
        {
            UpdateShape();
            _ = _hitShape.HitTest(ref m);
        }
    }

    protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
    {
        base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
        // the outlines and the region are rebuilt for the new DPI
        ResetShape();
        UpdateRegion();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ResetShape();
            _hitShape.Dispose();
        }
        base.Dispose(disposing);
    }
    #endregion

    #region Methods
    /// <summary>
    /// Generates a <see cref="System.Windows.Forms.Control.Click"/> event for the button, like <see cref="Button.PerformClick"/>:
    /// the focused control is validated first (when <see cref="System.Windows.Forms.Control.CausesValidation"/> is true), and
    /// nothing happens while the button is disabled or hidden. Unlike the inherited method, it also works when
    /// <see cref="Focusable"/> is false. <see cref="Form.AcceptButton"/>, <see cref="Form.CancelButton"/> and mnemonics use it.
    /// </summary>
    /// <remarks>
    /// This method hides <see cref="Button.PerformClick"/>, which is not virtual: the compiler picks it only for a call made
    /// through a <see cref="YANBtn"/> or <see cref="IButtonControl"/> reference in code compiled against YANF 1.1 or later.
    /// A call through a reference typed as <see cref="Button"/> (for example <c>((Button)yanBtn).PerformClick()</c> or a helper
    /// that takes a <see cref="Button"/>), and code compiled against YANF 1.0 and not rebuilt, still run
    /// <see cref="Button.PerformClick"/>, which does nothing while <see cref="Focusable"/> is false. Call it on a
    /// <see cref="YANBtn"/> or <see cref="IButtonControl"/> reference, or leave <see cref="Focusable"/> true.
    /// </remarks>
    public new void PerformClick()
    {
        if (GetStyle(Selectable))
        {
            base.PerformClick();
            return;
        }
        // Button.PerformClick does nothing unless the button can be selected: lend it the Selectable style for the call
        _is_SelectableLent = true;
        SetStyle(Selectable, true);
        try
        {
            base.PerformClick();
        }
        finally
        {
            if (_is_SelectableLent)
            {
                _is_SelectableLent = false;
                SetStyle(Selectable, false);
            }
        }
    }

    // Option display (the button takes the focus like a standard Button: see Focusable; TabStop and TabIndex keep their standard defaults)
    private void OptionDisplay()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = MediumSlateBlue;
        ForeColor = White;
        Size = new Size(150, 40);
        Font = new Font(Font.Name, 10f);
    }

    // Build the outlines for the current size, DPI, BorderSize and BorderRadius (once per change, never per paint), with the 1.x
    // geometry in device pixels: a radius above 2 pixels rounds the corners, and the rounded surface is inset by the ring where
    // 1.x painted Parent.BackColor over its region to smooth the edge (half the border rounded up, or one pixel without a border),
    // which now shows the parent; the border lies inside the surface. The configured values are never changed to fit
    private void UpdateShape()
    {
        var dpi = GetDpi(this);
        var rect = ClientRectangle;
        if (_is_ShapeBuilt && _shapeSize == rect.Size && _shapeDpi == dpi)
        {
            return;
        }
        ResetShape();
        _is_ShapeBuilt = true;
        _shapeSize = rect.Size;
        _shapeDpi = dpi;
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            _hitShape.Radius = 0;
            return;
        }
        _deviceBorderSize = Max(0, Min(LogicalToDevice(_borderSize, dpi), Min(rect.Width, rect.Height) / 2));
        var radius = (int)EffectiveRadius(rect, LogicalToDevice(_borderRadius, dpi));
        _is_Rounded = radius > LogicalToDevice(MIN_ROUND_RADIUS, dpi);
        _deviceRadius = _is_Rounded ? radius : 0;
        _hitShape.Radius = _is_Rounded ? _borderRadius : 0;
        if (_is_Rounded)
        {
            // half the border rounded up to whole pixels, as 1.x's pen covered them: an odd border stays crisp (BorderSize 1: one
            // row of the parent, then one row of border)
            _deviceRing = _deviceBorderSize > 0 ? (_deviceBorderSize + 1) / 2 : LogicalToDevice(EDGE_RING, dpi);
            var ring = (float)_deviceRing;
            var rectSurface = RectangleF.Inflate(rect, -ring, -ring);
            var radiusSurface = Max(0f, radius - ring);
            _pathSurface = RoundedRect(rectSurface, radiusSurface);
            _pathBorder = BorderRing(rectSurface, radiusSurface, _deviceBorderSize);
        }
        else
        {
            _pathBorder = BorderRing(rect, 0f, _deviceBorderSize);
        }
    }

    // Drop the outlines; they are rebuilt at the next use
    private void ResetShape()
    {
        _pathSurface?.Dispose();
        _pathBorder?.Dispose();
        _pathSurface = null;
        _pathBorder = null;
        _is_ShapeBuilt = false;
        _is_Rounded = false;
        _deviceRing = 0;
    }

    // Draw the keyboard focus cue: a dotted outline that follows the shape, inside the border
    private void DrawFocusCue(Graphics graphics)
    {
        var dpi = GetDpi(this);
        var rectSurface = ClientRectangle;
        // a rounded border lies inside the ring (half its size, rounded up); without a border the cue keeps its 1.x place
        var inset = (_is_Rounded && _deviceBorderSize > 0 ? _deviceRing : 0) + _deviceBorderSize + LogicalToDevice(FOCUS_CUE_GAP, dpi);
        // whole-pixel coordinates keep the one-pixel dotted line sharp (GDI+ centres pixels on whole coordinates)
        var rectCue = new RectangleF(rectSurface.X + inset, rectSurface.Y + inset, rectSurface.Width - 2 * inset - 1, rectSurface.Height - 2 * inset - 1);
        using var pathCue = RoundedRect(rectCue, _is_Rounded ? _deviceRadius - inset : 0);
        if (pathCue == null)
        {
            return;
        }
        using var penCue = new Pen(IsHighContrast ? SystemColors.ControlText : ForeColor, LogicalToDevice(1f, dpi))
        {
            DashStyle = Dot
        };
        var smoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = _is_Rounded ? SmoothingMode.AntiAlias : SmoothingMode.None;
        graphics.DrawPath(penCue, pathCue);
        graphics.SmoothingMode = smoothing;
    }

    // Update the region of a FlatStyle.System button, which Windows paints (its corners can only be cut), when its size, radius,
    // style or DPI changes, never while painting. The other styles paint the parent around their shape: no region, and a region
    // that the application set is left alone
    private void UpdateRegion()
    {
        if (base.FlatStyle == FlatStyle.System)
        {
            UpdateShape();
            if (_is_Rounded && _hitShape.Shape != null)
            {
                SetRegion(this, _hitShape.Shape);
                _is_RegionSet = true;
                return;
            }
        }
        if (_is_RegionSet)
        {
            _is_RegionSet = false;
            SetRegion(this, (Region)null);
        }
    }
    #endregion

    #region Nested types
    // Gives FlatAppearance.BorderSize the designer default of YANBtn, 0, the value the constructor sets (the framework declares 1):
    // the designer then writes a border size of 1 picked by the user, which 1.x lost, and no longer writes 0
    private sealed class FlatAppearanceDescriptionProvider : TypeDescriptionProvider
    {
        public FlatAppearanceDescriptionProvider(TypeDescriptionProvider parent) : base(parent)
        {
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) => new FlatAppearanceDescriptor(base.GetTypeDescriptor(objectType, instance));
    }

    // The properties of FlatAppearance, with BorderSize redeclared
    private sealed class FlatAppearanceDescriptor : CustomTypeDescriptor
    {
        public FlatAppearanceDescriptor(ICustomTypeDescriptor parent) : base(parent)
        {
        }

        public override PropertyDescriptorCollection GetProperties() => Redeclare(base.GetProperties());

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => Redeclare(base.GetProperties(attributes));

        private static PropertyDescriptorCollection Redeclare(PropertyDescriptorCollection properties)
        {
            var redeclared = new PropertyDescriptor[properties.Count];
            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];
                redeclared[i] = property.Name == nameof(FlatButtonAppearance.BorderSize) && property.PropertyType == typeof(int) ? new BorderSizeDescriptor(property) : property;
            }
            return new PropertyDescriptorCollection(redeclared, true);
        }
    }

    // FlatAppearance.BorderSize with the default 0 (a DefaultValue given here replaces the framework's)
    private sealed class BorderSizeDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor _inner;

        public BorderSizeDescriptor(PropertyDescriptor inner) : base(inner, new Attribute[] { new DefaultValueAttribute(0) }) => _inner = inner;

        public override Type ComponentType => _inner.ComponentType;

        public override bool IsReadOnly => _inner.IsReadOnly;

        public override Type PropertyType => _inner.PropertyType;

        public override bool CanResetValue(object component) => !IsDefault(component);

        public override object GetValue(object component) => _inner.GetValue(component);

        public override void ResetValue(object component) => SetValue(component, 0);

        public override void SetValue(object component, object value) => _inner.SetValue(component, value);

        public override bool ShouldSerializeValue(object component) => !IsDefault(component);

        // Check whether the value is the default, 0
        private bool IsDefault(object component) => Equals(GetValue(component), 0);
    }
    #endregion
}
