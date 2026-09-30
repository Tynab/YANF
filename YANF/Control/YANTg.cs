using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.DashStyle;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Windows.Forms.Cursors;
using static YANF.Script.YANShape;

namespace YANF.Control
{
    [ToolboxBitmap(typeof(CheckBox))]
    public class YANTg : CheckBox
    {
        #region Fields
        private Color _onBackColor = MediumSlateBlue;
        private Color _onToggleColor = WhiteSmoke;
        private Color _offBackColor = Gray;
        private Color _offToggleColor = Gainsboro;
        private bool _is_SolidStyle = true;
        #endregion

        #region Constructors
        public YANTg()
        {
            MinimumSize = new Size(45, 22);
        }
        #endregion

        #region Properties
        [Category("YAN Appearance"), Description("The color of the surface when the control is on.")]
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
        public Color OnBackColor
        {
            get => _onBackColor;
            set
            {
                if (_onBackColor != value)
                {
                    _onBackColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The color of the toggle when the control is on.")]
        [DefaultValue(typeof(Color), "WhiteSmoke")]
        public Color OnToggleColor
        {
            get => _onToggleColor;
            set
            {
                if (_onToggleColor != value)
                {
                    _onToggleColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The color of the surface when the control is off.")]
        [DefaultValue(typeof(Color), "Gray")]
        public Color OffBackColor
        {
            get => _offBackColor;
            set
            {
                if (_offBackColor != value)
                {
                    _offBackColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("The color of the toggle when the control is off.")]
        [DefaultValue(typeof(Color), "Gainsboro")]
        public Color OffToggleColor
        {
            get => _offToggleColor;
            set
            {
                if (_offToggleColor != value)
                {
                    _offToggleColor = value;
                    Invalidate();
                }
            }
        }

        [Category("YAN Appearance"), Description("When this property is true, the surface is filled with its color; otherwise only its outline is drawn.")]
        [DefaultValue(true)]
        public bool SolidStyle
        {
            get => _is_SolidStyle;
            set
            {
                if (_is_SolidStyle != value)
                {
                    _is_SolidStyle = value;
                    Invalidate();
                }
            }
        }
        #endregion

        #region Overridden
        [Category("YAN Appearance"), Description("Not used: the toggle displays no text, and setting it has no effect.")]
        [DefaultValue("")]
        public override string Text
        {
            get => base.Text;
            set { }
        }

        protected override Cursor DefaultCursor => Hand;

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = AntiAlias;
            graphics.Clear(Parent?.BackColor ?? BackColor);
            var tgSize = Height - 5;
            // the surface is a pill with half circles of diameter Height - 1 at both ends
            using var pathSurface = RoundedRect(new RectangleF(0, 0, Width - 2, Height - 1), (Height - 1) / 2f);
            using var brushSurface = new SolidBrush(Checked ? _onBackColor : _offBackColor);
            using var brushToggle = new SolidBrush(Checked ? _onToggleColor : _offToggleColor);
            // draw the control surface
            if (pathSurface != null)
            {
                if (_is_SolidStyle)
                {
                    graphics.FillPath(brushSurface, pathSurface);
                }
                else
                {
                    using var penSurface = new Pen(brushSurface.Color, 2);
                    graphics.DrawPath(penSurface, pathSurface);
                }
            }
            // draw the toggle
            if (tgSize > 0)
            {
                graphics.FillEllipse(brushToggle, Checked ? new Rectangle(Width - Height + 1, 2, tgSize, tgSize) : new Rectangle(2, 2, tgSize, tgSize));
            }
            // draw the keyboard focus cue (Windows hides it until the keyboard is used); ButtonBase repaints on focus changes.
            // It takes the toggle color in both styles: it contrasts with a solid surface, and on an outlined one it stands out
            // from the outline that it runs along (the surface color would only make the outline look thicker)
            if (Focused && ShowFocusCues)
            {
                DrawFocusCue(graphics, brushToggle.Color);
            }
        }
        #endregion

        #region Methods
        // Draw the keyboard focus cue: a dotted outline inside the surface, between its edge and the toggle
        private void DrawFocusCue(Graphics graphics, Color color)
        {
            // one pixel inside the edge of the surface and concentric with its ends; whole-pixel coordinates keep the one-pixel
            // dotted line sharp (with anti-aliasing, GDI+ centres pixels on whole coordinates)
            using var pathCue = RoundedRect(new RectangleF(1, 1, Width - 4, Height - 3), (Height - 3) / 2f);
            if (pathCue == null)
            {
                return;
            }
            using var penCue = new Pen(SystemInformation.HighContrast ? SystemColors.ControlText : color);
            penCue.DashStyle = Dot;
            graphics.DrawPath(penCue, pathCue);
        }
        #endregion
    }
}