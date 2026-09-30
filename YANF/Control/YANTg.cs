using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Color;
using static System.Drawing.Drawing2D.DashStyle;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Math;
using static System.Windows.Forms.Cursors;
using static YANF.Control.YANPaint;
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
        // position of the toggle: 0 = off (left), 1 = on (right); between the two while it slides
        private float _knob = 0f;
        private float _knobFrom = 0f;
        private float _knobTo = 0f;
        private long _slideStart;
        private double _slideMs;
        private System.Windows.Forms.Timer _timerSlide;
        // the toggle has been painted since it was last shown: before that (a form that is loading, where Form_Load restores the
        // settings while the windows exist and Visible is already true) a change of Checked is not seen, so the toggle jumps
        private bool _is_Painted = false;
        // the surface and focus cue outlines, built for _shapeSize at _shapeDpi
        private GraphicsPath _pathSurface;
        private GraphicsPath _pathCue;
        private Size _shapeSize;
        private int _shapeDpi;
        // the slide of the toggle between off and on, in milliseconds (ease-out)
        private const int SLIDE_MS = 150;
        // a frame of the slide (about 60 Hz; the timer rounds it up to the system timer tick)
        private const int SLIDE_FRAME_MS = 15;
        #endregion

        #region Constructors
        public YANTg()
        {
            MinimumSize = new Size(45, 22);
            // the parent is painted behind the control: repaint when it moves or when the parent changes behind it
            FollowParent(this);
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
            _is_Painted = true;
            // what is behind the control: the parent's own pixels (a gradient, an image), not a ring of Parent.BackColor
            PaintParent(this, e);
            graphics.SmoothingMode = AntiAlias;
            UpdateShape();
            var knob = GetKnob();
            GetColors(knob, out var colorSurface, out var colorToggle, out var isSolid);
            // draw the control surface
            if (_pathSurface != null)
            {
                if (isSolid)
                {
                    using var brushSurface = new SolidBrush(colorSurface);
                    graphics.FillPath(brushSurface, _pathSurface);
                }
                else
                {
                    using var penSurface = new Pen(colorSurface, LogicalToDevice(this, 2f));
                    graphics.DrawPath(penSurface, _pathSurface);
                }
            }
            // draw the toggle
            var rectToggle = GetToggleRect(knob);
            if (rectToggle.Width > 0)
            {
                using var brushToggle = new SolidBrush(colorToggle);
                graphics.FillEllipse(brushToggle, rectToggle);
            }
            // draw the keyboard focus cue (Windows hides it until the keyboard is used); ButtonBase repaints on focus changes.
            // It takes the toggle color in both styles: it contrasts with a solid surface, and on an outlined one it stands out
            // from the outline that it runs along (the surface color would only make the outline look thicker)
            if (Focused && ShowFocusCues)
            {
                DrawFocusCue(graphics, colorToggle);
            }
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            // the toggle starts from where it is drawn: where a slide in progress took it, or the rest position before the change
            StartSlide(IsSliding ? _knob : Checked ? 0f : 1f);
            base.OnCheckedChanged(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            // shown again (itself, its form, its tab page; WinForms tells the children of a hidden parent only when it is shown again)
            // or hidden: nothing slides until it has been painted, and a hidden toggle ends its slide
            _is_Painted = false;
            if (!Visible && IsSliding)
            {
                StopSlide();
            }
        }

        protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew)
        {
            base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
            // the outlines are rebuilt for the new DPI at the next paint
            ResetShape();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timerSlide?.Dispose();
                _timerSlide = null;
                ResetShape();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Methods
        // A slide of the toggle is in progress
        private bool IsSliding => _timerSlide != null && _timerSlide.Enabled;

        // Get the position of the toggle to draw: where the slide in progress is, otherwise the rest position of Checked
        private float GetKnob() => IsSliding ? _knob : Checked ? 1f : 0f;

        // Get the painted colors: blended between off and on while the toggle slides. In high contrast mode the system colors: off is
        // an outline and a toggle in the text color, on is a highlighted surface with a toggle in the highlighted text color
        private void GetColors(float knob, out Color colorSurface, out Color colorToggle, out bool isSolid)
        {
            if (IsHighContrast)
            {
                isSolid = Checked;
                colorSurface = Lerp(SystemColors.ControlText, SystemColors.Highlight, knob);
                colorToggle = Lerp(SystemColors.ControlText, SystemColors.HighlightText, knob);
                return;
            }
            isSolid = _is_SolidStyle;
            colorSurface = Lerp(_offBackColor, _onBackColor, knob);
            colorToggle = Lerp(_offToggleColor, _onToggleColor, knob);
        }

        // Get the toggle: a circle inset by 2 pixels in the surface, from the left end (off, 0) to the right end (on, 1) of its travel
        private RectangleF GetToggleRect(float knob)
        {
            var dpi = GetDpi(this);
            var inset = LogicalToDevice(2, dpi);
            var size = Height - LogicalToDevice(1, dpi) - 2 * inset;
            if (size <= 0)
            {
                return RectangleF.Empty;
            }
            var right = Width - LogicalToDevice(2, dpi) - inset - size;
            return new RectangleF(inset + (right - inset) * knob, inset, size, size);
        }

        // Build the outlines of the surface and of the focus cue for the current size and DPI (once per change, not per frame)
        private void UpdateShape()
        {
            var dpi = GetDpi(this);
            if (_shapeDpi == dpi && _shapeSize == Size)
            {
                return;
            }
            ResetShape();
            _shapeSize = Size;
            _shapeDpi = dpi;
            // the surface is a pill with half circles of diameter Height - 1 at both ends (2 and 1 are 96-dpi pixels)
            var rectSurface = new RectangleF(0, 0, Width - LogicalToDevice(2, dpi), Height - LogicalToDevice(1, dpi));
            _pathSurface = RoundedRect(rectSurface, rectSurface.Height / 2f);
            // the focus cue runs one pixel inside the edge of the surface, concentric with its ends; whole-pixel coordinates keep the
            // one-pixel dotted line sharp (with anti-aliasing, GDI+ centres pixels on whole coordinates)
            var inset = LogicalToDevice(1, dpi);
            var rectCue = RectangleF.Inflate(rectSurface, -inset, -inset);
            _pathCue = RoundedRect(rectCue, rectCue.Height / 2f);
        }

        // Drop the outlines; they are rebuilt at the next paint
        private void ResetShape()
        {
            _pathSurface?.Dispose();
            _pathCue?.Dispose();
            _pathSurface = null;
            _pathCue = null;
            _shapeDpi = 0;
        }

        // Draw the keyboard focus cue: a dotted outline inside the surface, between its edge and the toggle
        private void DrawFocusCue(Graphics graphics, Color color)
        {
            if (_pathCue == null)
            {
                return;
            }
            using var penCue = new Pen(color, LogicalToDevice(this, 1));
            penCue.DashStyle = Dot;
            graphics.DrawPath(penCue, _pathCue);
        }

        // Slide the toggle from a position to the rest position of Checked (ease-out). It jumps there when Windows animation effects
        // are off (reduced motion), while the control cannot be seen (no window, hidden, not painted yet: its form is loading) and in
        // the designer
        private void StartSlide(float from)
        {
            var to = Checked ? 1f : 0f;
            if (from == to || !IsAnimated || !_is_Painted || !IsHandleCreated || !Visible || DesignMode)
            {
                StopSlide();
                return;
            }
            _knob = _knobFrom = from;
            _knobTo = to;
            // a slide that starts halfway is shorter: the toggle keeps its speed
            _slideMs = SLIDE_MS * Abs(to - from);
            _slideStart = Stopwatch.GetTimestamp();
            if (_timerSlide == null)
            {
                _timerSlide = new System.Windows.Forms.Timer
                {
                    Interval = SLIDE_FRAME_MS
                };
                _timerSlide.Tick += TimerSlide_Tick;
            }
            _timerSlide.Start();
        }

        // End the slide at the rest position of Checked
        private void StopSlide()
        {
            _timerSlide?.Stop();
            _knob = Checked ? 1f : 0f;
            Invalidate();
        }

        // Move the toggle on the elapsed time (ease-out cubic), so that the slide lasts the same whatever the timer's pace
        private void TimerSlide_Tick(object sender, EventArgs e)
        {
            var t = Min(1d, (Stopwatch.GetTimestamp() - _slideStart) * 1000d / Stopwatch.Frequency / _slideMs);
            if (t >= 1d || IsDisposed)
            {
                StopSlide();
                return;
            }
            _knob = _knobFrom + (_knobTo - _knobFrom) * (float)(1d - Pow(1d - t, 3));
            Invalidate();
        }
        #endregion
    }
}
