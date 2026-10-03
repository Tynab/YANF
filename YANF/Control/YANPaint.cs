using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using YANF.Script;
using static System.Drawing.Drawing2D.PixelOffsetMode;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Math;
using static System.Windows.Forms.SystemInformation;

namespace YANF.Control
{
    using WinControl = System.Windows.Forms.Control;

    /// <summary>
    /// Shared painting helpers of the owner-drawn controls (2.0): real transparency (the parent's own pixels are painted behind the
    /// control, instead of a ring or a block of <c>Parent.BackColor</c>), anti-aliased shapes without a clipping region, DPI scaling
    /// of the pixel values, and the high-contrast and reduced-motion settings. <see cref="YANBorder"/> builds on it for the controls
    /// with a BorderSize / BorderRadius.
    /// </summary>
    /// <remarks>
    /// <para><b>Transparency.</b> Paint the parent first, then the control's shape anti-aliased on top of it, and let the control
    /// follow its parent from its constructor:</para>
    /// <code>
    /// public YANXxx()
    /// {
    ///     YANPaint.FollowParent(this);                  // repaint on Move and when the parent behind the control is invalidated
    /// }
    ///
    /// protected override void OnPaintBackground(PaintEventArgs e)   // or first in OnPaint for an Opaque control (ButtonBase)
    /// {
    ///     YANPaint.PaintParent(this, e);                // the parent's background and foreground: gradient, image, Paint handlers
    ///     YANPaint.FillPath(e.Graphics, BackColor, _shape);   // the anti-aliased surface (a path cached per size and DPI)
    /// }
    /// </code>
    /// <para>A control with a <see cref="YANBorder"/> fills its surface with <see cref="YANBorder.FillShape(Graphics, Color)"/>
    /// instead. All the fills and strokes of these helpers use the same pixel model (<see cref="SetCrisp"/>: pixel (x, y) covers the
    /// square from (x, y) to (x + 1, y + 1)), so a surface, a border and an underline built on the same rectangle line up exactly;
    /// paint a shape built for them through them, not with the default pixel offset of a <see cref="Graphics"/>.</para>
    /// <para>This is what WinForms itself does for a control with a transparent BackColor (<c>Control.PaintTransparentBackground</c>),
    /// with the same limitation: sibling controls that overlap the control are not painted behind it. The parent is painted with its
    /// own <c>OnPaintBackground</c> and <c>OnPaint</c>, through the device context moved to the parent's coordinates and clipped to
    /// the area of the control, so GDI drawing (<c>TextRenderer</c>, <c>GetHdc</c>) lands where GDI+ drawing does. The parent's
    /// <c>Paint</c> handlers therefore run once more whenever the control repaints, with <c>ClipRectangle</c> set to the area of the
    /// control: they should be free of side effects and honour <c>ClipRectangle</c>. Without gdi32 (Mono) the graphics is moved
    /// and clipped instead, which GDI drawing ignores unless it passes <c>TextFormatFlags.PreserveGraphicsTranslateTransform</c> and
    /// <c>PreserveGraphicsClipping</c>. Without a parent, or when the parent's painting paints the control again (re-entrancy), the
    /// control's BackColor is used instead. Behind a control with a non-client border (a <c>BorderStyle</c>) the parent is lined up
    /// with the control's client origin, not with its <c>Location</c>.</para>
    /// <para><b>No region.</b> A <see cref="Region"/> clips at whole pixels and cannot be anti-aliased, and assigning one repaints the
    /// window: paint the rounded shape over the painted parent instead. Keep a region only where a child window must be clipped
    /// (the inner TextBox of a rounded YANTxt) or where the corners must not take clicks (see <see cref="YANBorder.HitTest"/>), and
    /// build it when the size or the DPI changes, never in OnPaint (<see cref="YANShape.SetRegion(WinControl, GraphicsPath)"/>).</para>
    /// <para><b>DPI.</b> Designer values in pixels (BorderSize, BorderRadius, ChannelHeight...) and drawing constants are 96-dpi
    /// logical units. Convert them at paint time with <see cref="LogicalToDevice(WinControl, int)"/> (the rounding of
    /// <c>Control.LogicalToDeviceUnits</c>), cache what is built from them per client size and <see cref="GetDpi"/>, and drop the
    /// cache in an override of <c>RescaleConstantsForDpi</c>. DPI-unaware applications (the .NET Framework default) always run at
    /// 96 dpi, where nothing changes. Fonts are not scaled here: a font in points already follows the DPI, and AutoScale and the
    /// WinForms DPI handling rescale a control's own font; a font created in a constructor must stay in points.</para>
    /// <para><b>Accessibility.</b> <see cref="Contrast"/> picks a system color in high contrast mode, and <see cref="IsAnimated"/> is
    /// false when Windows animation effects are off (reduced motion, either setting): jump to the final state instead of animating.</para>
    /// </remarks>
    internal static class YANPaint
    {
        #region Fields
        /// <summary>
        /// The DPI of the logical units (designer values and drawing constants).
        /// </summary>
        public const int LOGICAL_DPI = 96;
        private const int SPI_GETCLIENTAREAANIMATION = 0x1042;
        // Controls painting their parent on this thread (re-entrancy guard)
        [ThreadStatic]
        private static List<WinControl> _parentPainters;
        // Per-thread test overrides (null: follow the control and Windows)
        [ThreadStatic]
        private static int? _dpiOverride;
        [ThreadStatic]
        private static bool? _highContrastOverride;
        // Control.DeviceDpi exists on .NET Framework 4.7 and later and on .NET; a WinForms without it (Mono) paints at 96 dpi
        private static readonly bool _is_DeviceDpiSupported = typeof(WinControl).GetProperty("DeviceDpi") != null;
        // The gdi32 device context calls exist: 0 not probed yet, 1 yes, -1 no (Mono maps gdi32 to libgdiplus, which has none of them)
        private static int _dcSupport = 0;
        // SystemParametersInfo exists (Windows); cleared at the first failure (Mono has no user32)
        private static bool _is_SpiSupported = true;
        private static ParentPainter _parentPainter;
        #endregion

        #region Properties
        /// <summary>
        /// Lets tests paint at another DPI (per thread; null uses the control's <c>DeviceDpi</c>).
        /// </summary>
        internal static int? DpiOverride
        {
            get => _dpiOverride;
            set => _dpiOverride = value;
        }

        /// <summary>
        /// Lets tests paint in high contrast mode or not (per thread; null follows <see cref="SystemInformation.HighContrast"/>).
        /// </summary>
        internal static bool? HighContrastOverride
        {
            get => _highContrastOverride;
            set => _highContrastOverride = value;
        }

        /// <summary>
        /// True when Windows runs in high contrast mode: paint with system colors (see <see cref="Contrast"/>).
        /// </summary>
        public static bool IsHighContrast => _highContrastOverride ?? HighContrast;

        /// <summary>
        /// True when Windows animation effects are on; false means reduced motion: show the final state at once. Both switches count:
        /// the UI effects of the performance options (<see cref="SystemInformation.UIEffectsEnabled"/>) and the "Animation effects" of
        /// Windows 10 and 11 (Settings &gt; Accessibility &gt; Visual effects, SPI_GETCLIENTAREAANIMATION, what browsers report as
        /// prefers-reduced-motion). Tests set <see cref="YANDisplay.UIEffectsOverride"/>, shared with the fades.
        /// </summary>
        public static bool IsAnimated => YANDisplay.UIEffectsOverride ?? (UIEffectsEnabled && IsClientAreaAnimated());
        #endregion

        #region Methods
        /// <summary>
        /// Gets the DPI the control paints at: its <c>DeviceDpi</c> (the monitor's DPI in a per-monitor aware application, the system
        /// DPI in a system-aware one, 96 in a DPI-unaware one).
        /// </summary>
        public static int GetDpi(WinControl ctrl) => _dpiOverride ?? (ctrl != null && _is_DeviceDpiSupported ? GetDeviceDpi(ctrl) : LOGICAL_DPI);

        /// <summary>
        /// Converts a 96-dpi logical value to device pixels at the given DPI, rounded like <c>Control.LogicalToDeviceUnits</c>.
        /// </summary>
        public static int LogicalToDevice(int value, int dpi)
        {
            if (dpi == LOGICAL_DPI || dpi <= 0)
            {
                return value;
            }
            // huge designer values (a radius of int.MaxValue) must not overflow
            return (int)Max(int.MinValue, Min(int.MaxValue, Round(dpi / (double)LOGICAL_DPI * value)));
        }

        /// <summary>
        /// Converts a 96-dpi logical value to device pixels at the given DPI, without rounding (pen widths, sub-pixel offsets).
        /// </summary>
        public static float LogicalToDevice(float value, int dpi) => dpi == LOGICAL_DPI || dpi <= 0 ? value : value * dpi / LOGICAL_DPI;

        /// <summary>
        /// Converts a 96-dpi logical value to device pixels at the DPI of the control (see <see cref="GetDpi"/>).
        /// </summary>
        public static int LogicalToDevice(WinControl ctrl, int value) => LogicalToDevice(value, GetDpi(ctrl));

        /// <summary>
        /// Converts a 96-dpi logical value to device pixels at the DPI of the control, without rounding.
        /// </summary>
        public static float LogicalToDevice(WinControl ctrl, float value) => LogicalToDevice(value, GetDpi(ctrl));

        /// <summary>
        /// Returns <paramref name="highContrastColor"/> (a <see cref="SystemColors"/> member) in high contrast mode, otherwise
        /// <paramref name="color"/>. The configured color itself is never changed.
        /// </summary>
        public static Color Contrast(Color color, Color highContrastColor) => IsHighContrast ? highContrastColor : color;

        /// <summary>
        /// Blends two colors (alpha included); <paramref name="t"/> is clamped to [0, 1], and 0 and 1 return the colors themselves.
        /// </summary>
        public static Color Lerp(Color from, Color to, float t)
        {
            if (t <= 0f || from == to)
            {
                return from;
            }
            if (t >= 1f)
            {
                return to;
            }
            static int Mix(int a, int b, float t) => (int)Round(a + (b - a) * t);
            return Color.FromArgb(Mix(from.A, to.A, t), Mix(from.R, to.R, t), Mix(from.G, to.G, t), Mix(from.B, to.B, t));
        }

        /// <summary>
        /// Paints what is behind the control, clipped to the area to paint: the parent's background and foreground (its
        /// <c>OnPaintBackground</c> and <c>OnPaint</c>, in the parent's coordinates, through the device context like WinForms does for a
        /// transparent BackColor). Without a parent, or when this control is already painting its parent on this thread (the parent's
        /// painting paints the control again), the control's BackColor is used (over <see cref="SystemColors.Control"/> when it is not
        /// opaque). The state of the graphics and of its device context is restored afterwards.
        /// </summary>
        public static void PaintParent(WinControl ctrl, PaintEventArgs e) => PaintParent(ctrl, e.Graphics, e.ClipRectangle);

        /// <summary>
        /// Paints what is behind a part of the control (in client coordinates): see <see cref="PaintParent(WinControl, PaintEventArgs)"/>.
        /// </summary>
        public static void PaintParent(WinControl ctrl, Graphics graphics, Rectangle clip)
        {
            clip.Intersect(new Rectangle(Point.Empty, ctrl.ClientSize));
            // and the clip of the graphics (a huge rectangle when it has none)
            var bounds = graphics.ClipBounds;
            clip.Intersect(Rectangle.FromLTRB((int)Floor(bounds.Left), (int)Floor(bounds.Top), (int)Ceiling(bounds.Right), (int)Ceiling(bounds.Bottom)));
            if (clip.Width <= 0 || clip.Height <= 0)
            {
                return;
            }
            var parent = ctrl.Parent;
            var painters = _parentPainters ??= new List<WinControl>();
            if (parent == null || parent.IsDisposed || painters.Contains(ctrl))
            {
                PaintBackColor(ctrl, graphics, clip);
                return;
            }
            painters.Add(ctrl);
            try
            {
                // the parent paints in its own client coordinates, only over the area of the control
                var origin = GetClientOrigin(ctrl, parent);
                var parentClip = clip;
                parentClip.Offset(origin);
                if (!PaintParentOnDc(parent, graphics, clip, origin, parentClip))
                {
                    PaintParentOnGraphics(parent, graphics, origin, parentClip);
                }
            }
            finally
            {
                _ = painters.Remove(ctrl);
            }
        }

        /// <summary>
        /// Keeps the parent painted behind the control up to date, as WinForms does for a control with a transparent BackColor:
        /// the control is repainted when it moves, and when the part of its parent behind it is invalidated (a gradient or color
        /// change of the parent that does not invalidate its children). Call it once, from the constructor.
        /// </summary>
        public static void FollowParent(WinControl ctrl) => _ = new ParentLink(ctrl);

        /// <summary>
        /// Fills the path anti-aliased with a solid color (nothing for a null path or a fully transparent color), in the pixel model
        /// of <see cref="SetCrisp"/> like <see cref="YANBorder"/>. The smoothing and pixel offset modes of the graphics are restored
        /// afterwards.
        /// </summary>
        public static void FillPath(Graphics graphics, Color color, GraphicsPath path)
        {
            if (path == null || color.A == 0)
            {
                return;
            }
            var smoothing = graphics.SmoothingMode;
            var pixelOffset = graphics.PixelOffsetMode;
            try
            {
                SetCrisp(graphics);
                using var brush = new SolidBrush(color);
                graphics.FillPath(brush, path);
            }
            finally
            {
                graphics.SmoothingMode = smoothing;
                graphics.PixelOffsetMode = pixelOffset;
            }
        }

        /// <summary>
        /// Strokes the path anti-aliased with a solid pen (nothing for a null path, a width below or at 0 or a fully transparent
        /// color), in the pixel model of <see cref="SetCrisp"/>: a pen of width w along <see cref="BorderPath"/> covers exactly the
        /// border band. The smoothing and pixel offset modes of the graphics are restored afterwards.
        /// </summary>
        public static void DrawPath(Graphics graphics, Color color, float width, GraphicsPath path)
        {
            if (path == null || width <= 0f || color.A == 0)
            {
                return;
            }
            var smoothing = graphics.SmoothingMode;
            var pixelOffset = graphics.PixelOffsetMode;
            try
            {
                SetCrisp(graphics);
                using var pen = new Pen(color, width);
                graphics.DrawPath(pen, path);
            }
            finally
            {
                graphics.SmoothingMode = smoothing;
                graphics.PixelOffsetMode = pixelOffset;
            }
        }

        /// <summary>
        /// Sets the pixel model of the shared helpers: anti-aliasing, with pixel (x, y) covering the square from (x, y) to
        /// (x + 1, y + 1) (<see cref="PixelOffsetMode.Half"/>), so straight edges on whole coordinates stay crisp and a shape built on a
        /// rectangle covers exactly its pixels. The caller saves and restores the state of the graphics.
        /// </summary>
        public static void SetCrisp(Graphics graphics)
        {
            graphics.SmoothingMode = AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.Half; // qualified: .NET 5+ also has System.Half
        }

        /// <summary>
        /// Builds the centre line of a border of the given width that lies inside <paramref name="rect"/> and follows its rounded
        /// corners: the rectangle inset by half the width, with the (clamped) radius reduced by half the width, never below 0.
        /// Stroke it with a pen of that width for a dashed or gradient border; for a solid border fill <see cref="BorderRing"/>,
        /// whose edges stay crisp (libgdiplus moves strokes by half a pixel). Null when nothing fits.
        /// </summary>
        public static GraphicsPath BorderPath(RectangleF rect, float radius, float width)
        {
            if (width <= 0f)
            {
                return null;
            }
            var half = width / 2f;
            var inner = RectangleF.Inflate(rect, -half, -half);
            return YANShape.RoundedRect(inner, Max(0f, YANShape.EffectiveRadius(rect, radius) - half));
        }

        /// <summary>
        /// Builds the band of a border of the given width along the inside of <paramref name="rect"/>: its rounded outline plus the
        /// outline inset by the width (radius reduced by the width, never below 0), filled with the default alternate fill mode.
        /// Filling it gives the same band as stroking <see cref="BorderPath"/>, with edges that are crisp on every GDI+ implementation.
        /// The whole shape when the width reaches the middle; null when nothing fits.
        /// </summary>
        public static GraphicsPath BorderRing(RectangleF rect, float radius, float width)
        {
            if (width <= 0f)
            {
                return null;
            }
            var effectiveRadius = YANShape.EffectiveRadius(rect, radius);
            var ring = YANShape.RoundedRect(rect, effectiveRadius);
            if (ring == null)
            {
                return null;
            }
            using var inner = YANShape.RoundedRect(RectangleF.Inflate(rect, -width, -width), Max(0f, effectiveRadius - width));
            if (inner != null)
            {
                ring.AddPath(inner, false);
            }
            return ring;
        }

        // Fill with the control's own BackColor where no parent can be painted
        private static void PaintBackColor(WinControl ctrl, Graphics graphics, Rectangle clip)
        {
            var backColor = ctrl.BackColor;
            if (backColor.A < 255)
            {
                graphics.FillRectangle(SystemBrushes.Control, clip);
            }
            if (backColor.A > 0)
            {
                using var brush = new SolidBrush(backColor);
                graphics.FillRectangle(brush, clip);
            }
        }

        // Paint the parent through the device context, moved to the parent's coordinates and clipped to the area of the control there,
        // as Control.PaintTransparentBackground does: GDI drawing of the parent (TextRenderer, GetHdc in a Paint handler) ignores the
        // transform and the clip of a Graphics. False when it cannot be done: no gdi32 (Mono), or a graphics that is scaled or rotated
        private static bool PaintParentOnDc(WinControl parent, Graphics graphics, Rectangle clip, Point origin, Rectangle parentClip)
        {
            if (!IsDcSupported() || !TryGetDeviceOffset(graphics, out var offset))
            {
                return false;
            }
            var hdc = graphics.GetHdc();
            try
            {
                var saved = SaveDCNative(hdc);
                if (saved == 0)
                {
                    return false;
                }
                try
                {
                    // the clip in the current coordinates of the device context, then its origin moved onto the parent's
                    clip.Offset(offset);
                    _ = IntersectClipRectNative(hdc, clip.Left, clip.Top, clip.Right, clip.Bottom);
                    _ = OffsetViewportOrgExNative(hdc, offset.X - origin.X, offset.Y - origin.Y, IntPtr.Zero);
                    using var parentGraphics = Graphics.FromHdc(hdc);
                    using var pea = new PaintEventArgs(parentGraphics, parentClip);
                    (_parentPainter ??= new ParentPainter()).PaintParent(parent, pea);
                }
                finally
                {
                    _ = RestoreDCNative(hdc, saved);
                }
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }
            return true;
        }

        // Paint the parent through the graphics, moved to the parent's coordinates and clipped to the area of the control there
        // (GDI drawing of the parent is neither moved nor clipped unless it asks for it with TextFormatFlags.PreserveGraphics*)
        private static void PaintParentOnGraphics(WinControl parent, Graphics graphics, Point origin, Rectangle parentClip)
        {
            var state = graphics.Save();
            try
            {
                graphics.TranslateTransform(-origin.X, -origin.Y);
                graphics.IntersectClip(parentClip);
                using var pea = new PaintEventArgs(graphics, parentClip);
                (_parentPainter ??= new ParentPainter()).PaintParent(parent, pea);
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        // Get where the world origin of the graphics is on its device, when the graphics only moves it (a device context can be moved,
        // not scaled or rotated): the translation of a double buffer or of a caller, with the page unit and scale
        private static bool TryGetDeviceOffset(Graphics graphics, out Point offset)
        {
            var points = new[] { PointF.Empty, new PointF(1f, 0f), new PointF(0f, 1f) };
            graphics.TransformPoints(CoordinateSpace.Device, CoordinateSpace.World, points);
            offset = Point.Round(points[0]);
            static bool Near(float a, float b) => Abs(a - b) < 0.001f;
            return Near(points[1].X - points[0].X, 1f) && Near(points[1].Y, points[0].Y) && Near(points[2].X, points[0].X) && Near(points[2].Y - points[0].Y, 1f);
        }

        // Get the client origin of the control in the client coordinates of its parent: its Location, moved by its non-client border
        // when it has one (a BorderStyle, a scroll bar on the left). On the thread of the control only: that reads the window handles
        private static Point GetClientOrigin(WinControl ctrl, WinControl parent)
        {
            if (ctrl.ClientSize != ctrl.Size && ctrl.IsHandleCreated && parent.IsHandleCreated)
            {
                return parent.PointToClient(ctrl.PointToScreen(Point.Empty));
            }
            return ctrl.Location;
        }

        // The gdi32 device context calls exist: probed once with a call that fails harmlessly on Windows (no device context)
        private static bool IsDcSupported()
        {
            if (_dcSupport == 0)
            {
                try
                {
                    _ = SaveDCNative(IntPtr.Zero);
                    _dcSupport = 1;
                }
                catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                {
                    _dcSupport = -1;
                }
            }
            return _dcSupport > 0;
        }

        // The "Animation effects" switch of Windows 10 and 11 is on; true where it cannot be read (Mono, a Windows without it)
        private static bool IsClientAreaAnimated()
        {
            if (!_is_SpiSupported)
            {
                return true;
            }
            try
            {
                return !SystemParametersInfoNative(SPI_GETCLIENTAREAANIMATION, 0, out var isOn, 0) || isOn;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                _is_SpiSupported = false;
                return true;
            }
        }

        // Kept apart so that a WinForms without Control.DeviceDpi never compiles the call
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetDeviceDpi(WinControl ctrl) => ctrl.DeviceDpi;

        [DllImport("gdi32.dll", EntryPoint = "SaveDC")]
        private static extern int SaveDCNative(IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "RestoreDC")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RestoreDCNative(IntPtr hdc, int nSavedDC);

        [DllImport("gdi32.dll", EntryPoint = "IntersectClipRect")]
        private static extern int IntersectClipRectNative(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport("gdi32.dll", EntryPoint = "OffsetViewportOrgEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OffsetViewportOrgExNative(IntPtr hdc, int nXOffset, int nYOffset, IntPtr lpPoint);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoNative(int uiAction, int uiParam, [MarshalAs(UnmanagedType.Bool)] out bool pvParam, int fWinIni);
        #endregion

        #region Nested types
        // Calls the protected Control.InvokePaintBackground and InvokePaint for another control (it never creates a window)
        private sealed class ParentPainter : WinControl
        {
            public ParentPainter() => GC.SuppressFinalize(this);

            // Both layers from the same graphics state, as WinForms paints them: what the background leaves changed (a transform,
            // a clip, a smoothing mode) does not leak into the foreground
            public void PaintParent(WinControl parent, PaintEventArgs e)
            {
                var graphics = e.Graphics;
                var state = graphics.Save();
                InvokePaintBackground(parent, e);
                graphics.Restore(state);
                InvokePaint(parent, e);
            }
        }

        // Repaints a control that paints its parent when it moves or when its parent is invalidated behind it
        private sealed class ParentLink
        {
            private readonly WinControl _ctrl;
            private WinControl _parent;

            public ParentLink(WinControl ctrl)
            {
                _ctrl = ctrl;
                ctrl.Move += Ctrl_Move;
                ctrl.ParentChanged += Ctrl_ParentChanged;
                Ctrl_ParentChanged(ctrl, EventArgs.Empty);
            }

            // The pixels of the old location were copied along: paint the parent at the new one
            private void Ctrl_Move(object sender, EventArgs e) => _ctrl.Invalidate();

            private void Ctrl_ParentChanged(object sender, EventArgs e)
            {
                if (_parent != null)
                {
                    _parent.Invalidated -= Parent_Invalidated;
                }
                _parent = _ctrl.Parent;
                if (_parent != null)
                {
                    _parent.Invalidated += Parent_Invalidated;
                }
            }

            // The invalidated part of the parent, moved into the control's coordinates (what WinForms does for transparent children)
            private void Parent_Invalidated(object sender, InvalidateEventArgs e)
            {
                if (!_ctrl.IsHandleCreated)
                {
                    return;
                }
                // Control.Invalidate may be called from another thread, which raises Invalidated on that thread: the client origin of a
                // control with a non-client border needs its window handle (PointToScreen), which throws there when cross-thread calls are
                // checked (under a debugger), so the whole control is repainted instead (Invalidate itself is safe on any thread, as for
                // the parent). InvokeRequired is only asked for such a control: the usual path makes no extra call into Windows
                if (_ctrl.ClientSize != _ctrl.Size && _ctrl.InvokeRequired)
                {
                    _ctrl.Invalidate();
                    return;
                }
                var rect = e.InvalidRect;
                var origin = GetClientOrigin(_ctrl, _parent);
                rect.Offset(-origin.X, -origin.Y);
                rect.Intersect(_ctrl.ClientRectangle);
                if (rect.Width > 0 && rect.Height > 0)
                {
                    _ctrl.Invalidate(rect);
                }
            }
        }
        #endregion
    }
}
