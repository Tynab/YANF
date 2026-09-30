using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.DashStyle;
using static System.Drawing.Drawing2D.PenAlignment;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Drawing.Rectangle;
using static System.Math;
using static System.Windows.Forms.ControlStyles;
using static System.Windows.Forms.Cursors;
using static System.Windows.Forms.FlatStyle;
using static YANF.Script.YANShape;

namespace YANF.Control;

// IButtonControl is re-implemented so that Form.AcceptButton and Form.CancelButton call the PerformClick below
[ToolboxBitmap(typeof(Button))]
public partial class YANBtn : Button, IButtonControl
{
    #region Fields
    private Color _borderColor = PaleVioletRed;
    private int _borderSize = 0;
    private int _borderRadius = 20;
    private bool _is_PaintingBase = false;
    private bool _is_SelectableLent = false;
    private const int FOCUS_CUE_GAP = 3;
    #endregion

    #region Constructors
    public YANBtn()
    {
        OptionDisplay();
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
                Invalidate();
            }
        }
    }

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
                UpdateRegion();
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the button can receive the focus (it wraps <see cref="ControlStyles.Selectable"/>).
    /// When true, the button behaves like a standard <see cref="Button"/>: a click moves the focus to it (so the previously
    /// focused control raises Leave and Validating first), TAB reaches it when <see cref="System.Windows.Forms.Control.TabStop"/>
    /// is also true, SPACE and ENTER click it while it has the focus, and a focus cue is drawn inside its shape.
    /// The default, false, keeps the 1.0 behaviour: the button never takes the focus. <see cref="PerformClick"/>,
    /// <see cref="Form.AcceptButton"/>, <see cref="Form.CancelButton"/> and mnemonics work either way.
    /// </summary>
    /// <remarks>
    /// Designer files written by YANF 1.0 set <c>TabStop = false</c>; set it to true as well to reach such a button with TAB.
    /// While this property is false, a <see cref="Button.PerformClick"/> call that is not bound to <see cref="YANBtn"/> (see the
    /// remarks of <see cref="PerformClick"/>) does nothing, as in 1.0.
    /// </remarks>
    [Category("YAN Behavior"), Description("Indicates whether the button can receive the focus: a click then moves the focus to it, TAB reaches it (when TabStop is true) and SPACE or ENTER click it.")]
    [DefaultValue(false)]
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
    #endregion

    #region Overridden
    protected override Cursor DefaultCursor => Hand;

    /// <summary>
    /// Gets a value indicating whether the control should display focus cues. False while the base class paints: the flat
    /// style's rectangular focus cue would be cut off by the rounded region, so <see cref="OnPaint"/> draws one inside the shape.
    /// </summary>
    protected override bool ShowFocusCues => !_is_PaintingBase && base.ShowFocusCues;

    protected override void OnPaint(PaintEventArgs e)
    {
        // the base paints the flat button without its focus rectangle (see ShowFocusCues)
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
        var rectSurface = ClientRectangle;
        var borderRadius = GetBorderRadius();
        var borderSize = GetBorderSize();
        if (borderRadius > 2)
        {
            using var pathSurface = RoundedRect(rectSurface, borderRadius);
            using var pathBorder = RoundedRect(Inflate(rectSurface, -borderSize, -borderSize), borderRadius - borderSize);
            using var penSurface = new Pen(Parent?.BackColor ?? BackColor, borderSize > 0 ? borderSize : 2);
            using var penBorder = new Pen(GetBorderColor(), borderSize);
            graphics.SmoothingMode = AntiAlias;
            // draw surface
            graphics.DrawPath(penSurface, pathSurface);
            // draw border
            if (borderSize >= 1 && pathBorder != null)
            {
                graphics.DrawPath(penBorder, pathBorder);
            }
        }
        else
        {
            graphics.SmoothingMode = None;
            // draw border
            if (borderSize >= 1 && Width > 1 && Height > 1)
            {
                using var penBorder = new Pen(GetBorderColor(), borderSize);
                penBorder.Alignment = Inset;
                graphics.DrawRectangle(penBorder, 0, 0, Width - 1, Height - 1);
            }
        }
        // draw the keyboard focus cue (Windows hides it until the keyboard is used)
        if (Focused && ShowFocusCues)
        {
            DrawFocusCue(graphics, rectSurface, borderRadius, borderSize);
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
    /// <see cref="YANBtn"/> or <see cref="IButtonControl"/> reference, or set <see cref="Focusable"/> to true.
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

    // Option display (the button does not take the focus: see Focusable; TabStop and TabIndex keep their standard defaults)
    private void OptionDisplay()
    {
        SetStyle(Selectable, false);
        FlatStyle = Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = MediumSlateBlue;
        ForeColor = White;
        Size = new Size(150, 40);
        Font = new Font(Font.Name, 10f);
    }

    // Get the border radius that fits the current size, in whole pixels (the configured value is never changed)
    private int GetBorderRadius() => (int)EffectiveRadius(ClientRectangle, _borderRadius);

    // Get the border size that fits the current size (the configured value is never changed)
    private int GetBorderSize() => Max(0, Min(_borderSize, Min(Width, Height) / 2));

    // Get the painted border color: the system frame color in high contrast mode (the configured value is never changed)
    private Color GetBorderColor() => SystemInformation.HighContrast ? SystemColors.WindowFrame : _borderColor;

    // Draw the keyboard focus cue: a dotted outline that follows the shape, inside the border
    private void DrawFocusCue(Graphics graphics, Rectangle rectSurface, int borderRadius, int borderSize)
    {
        // a rounded border is centred on a path inset by its size, so it reaches 1.5 times its size into the control
        var inset = (borderRadius > 2 ? borderSize * 3 / 2 : borderSize) + FOCUS_CUE_GAP;
        // whole-pixel coordinates keep the one-pixel dotted line sharp (with anti-aliasing, GDI+ centres pixels on whole coordinates)
        var rectCue = new RectangleF(rectSurface.X + inset, rectSurface.Y + inset, rectSurface.Width - 2 * inset - 1, rectSurface.Height - 2 * inset - 1);
        using var pathCue = RoundedRect(rectCue, borderRadius > 2 ? borderRadius - inset : 0);
        if (pathCue == null)
        {
            return;
        }
        using var penCue = new Pen(SystemInformation.HighContrast ? SystemColors.ControlText : ForeColor);
        penCue.DashStyle = Dot;
        graphics.DrawPath(penCue, pathCue);
    }

    // Update the region of the control when its size or radius changes, never while painting
    private void UpdateRegion()
    {
        var borderRadius = GetBorderRadius();
        if (borderRadius > 2)
        {
            using var pathRegion = RoundedRect(ClientRectangle, borderRadius);
            SetRegion(this, pathRegion);
        }
        else
        {
            SetRegion(this, (Region)null);
        }
    }
    #endregion
}