using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using YANF.Control;
using static System.ComponentModel.EditorBrowsableState;
using static System.Drawing.FontStyle;
using static System.Math;
using static System.Runtime.InteropServices.CharSet;
using static System.Threading.Thread;
using static System.Windows.Forms.SystemInformation;
using static YANF.Script.YANConstant;

namespace YANF.Script
{
    public static class YANDisplay
    {
        #region Fields
        // Delay between two fade frames (about one 60 Hz frame; Task.Delay rounds it up to the system timer tick)
        private const int FADE_FRAME_MS = 15;
        // Fonts created by HighLightLblLinkByCtrl, per label (only these may be disposed when replaced)
        private static readonly ConditionalWeakTable<Label, Font> _highLightFonts = new();
        // EnableFade settings and progress, per form
        private static readonly ConditionalWeakTable<Form, FadeState> _fades = new();
        // Per-thread override of the Windows animation setting (null: follow SystemInformation.UIEffectsEnabled)
        [ThreadStatic]
        private static bool? _uiEffectsOverride;
        #endregion

        #region Properties
        /// <summary>
        /// Lets tests run the fades with or without animation whatever the machine's setting is (per thread; null follows Windows).
        /// </summary>
        internal static bool? UIEffectsOverride
        {
            get => _uiEffectsOverride;
            set => _uiEffectsOverride = value;
        }

        // Windows animation effects are on (the Performance Options setting; off in many remote sessions)
        // Same rule as the controls: UI effects and the Windows 10/11 "Animation effects" switch (reduced motion)
        private static bool IsAnimated => YANF.Control.YANPaint.IsAnimated;
        #endregion

        #region Methods
        /// <summary>
        /// Tạo khung ellipse cho form.
        /// </summary>
        /// <remarks>
        /// Raw GDI import kept for existing code. The returned region handle must be released with DeleteObject;
        /// <see cref="SetRoundRegion"/> does that for you.
        /// </remarks>
        [EditorBrowsable(Never)]
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        /// <summary>
        /// Điều khiển object với animation đồng bộ.
        /// </summary>
        /// <remarks>
        /// Raw user32 import kept for existing code. It blocks the UI thread for the whole animation; use <see cref="FadeToAsync"/>
        /// or <see cref="EnableFade"/> for fades.
        /// </remarks>
        [EditorBrowsable(Never)]
        [DllImport("user32.dll", CharSet = Auto)]
        public static extern void AnimateWindow(IntPtr hWnd, int time, AnimateWindowFlags flags);

        /// <summary>
        /// Fade in form.
        /// </summary>
        public static void FadeIn(this Form frm)
        {
            while (frm.Opacity < 1)
            {
                frm.Opacity += 0.05;
                frm.Update();
                Sleep(10);
            }
        }

        /// <summary>
        /// Fade out form.
        /// </summary>
        public static void FadeOut(this Form frm)
        {
            while (frm.Opacity > 0)
            {
                frm.Opacity -= 0.05;
                frm.Update();
                Sleep(10);
            }
        }

        /// <summary>
        /// Fades the form to <paramref name="opacity"/> in <paramref name="duration"/> milliseconds without blocking the UI thread.
        /// </summary>
        /// <param name="frm">Form to fade. Call this on the thread that owns the form.</param>
        /// <param name="opacity">Target opacity, clamped to [0, 1].</param>
        /// <param name="duration">Fade duration in milliseconds.</param>
        /// <returns>A task that completes when the form has reached the target opacity, or earlier when the form is disposed.</returns>
        /// <remarks>
        /// The opacity follows an ease-out curve over the elapsed time (measured with a <see cref="Stopwatch"/>), so the length of the
        /// fade does not depend on the timer resolution. Between frames it awaits a short <see cref="Task.Delay(int)"/>, so the message
        /// loop keeps running (input, painting, other forms). The target is applied at once when <paramref name="duration"/> is 0 or less
        /// or when Windows animation effects are turned off (<see cref="SystemInformation.UIEffectsEnabled"/> or the Windows 10/11
        /// "Animation effects" switch). A disposed form is left
        /// alone, including one disposed during the fade. Unlike <see cref="FadeIn"/> and <see cref="FadeOut"/>, this method returns at
        /// once: await the task where the next step must wait for the fade.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="frm"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="opacity"/> is NaN.</exception>
        public static Task FadeToAsync(this Form frm, double opacity, int duration)
        {
            if (frm is null)
            {
                throw new ArgumentNullException(nameof(frm));
            }
            if (double.IsNaN(opacity))
            {
                throw new ArgumentOutOfRangeException(nameof(opacity), opacity, "The opacity must be a number.");
            }
            return FadeAsync(frm, Max(0d, Min(1d, opacity)), duration, CancellationToken.None);
        }

        /// <summary>
        /// Fades the form in each time it is shown and out when it is closed, without blocking the UI thread.
        /// </summary>
        /// <param name="frm">Form to animate; typically called once in its constructor, after InitializeComponent.</param>
        /// <param name="fadeInDuration">Fade-in duration in milliseconds; 0 or less shows the form without a fade.</param>
        /// <param name="fadeOutDuration">Fade-out duration in milliseconds; 0 or less closes the form without a fade.</param>
        /// <remarks>
        /// <para>The form fades in to the opacity it had when this was first called (1 when that was 0, as in forms designed for
        /// <see cref="FadeIn"/>). When called before the form is shown, the form is set to opacity 0 right away, so it never flashes at
        /// full opacity before the fade starts; when called on a form already shown at opacity 0, the fade-in starts at once.</para>
        /// <para>On close, the close is postponed until the form has faded out, then it is closed again; FormClosing is raised a second
        /// time with the fade already done, and FormClosed once. A modal form keeps its DialogResult (WinForms resets it when a close is
        /// cancelled). There is no fade when another FormClosing handler or validation has already cancelled the close; when the close
        /// comes from Application.Exit, Windows shutdown, the Task Manager, or the owner or MDI parent closing; for an MDI child; and
        /// for a form that has MDI children or owned forms open (they would see FormClosing twice). FormClosing handlers added before
        /// the form is shown run before the fade decision; if a handler cancels the second close, the form fades back in.</para>
        /// <para>Nothing is animated when Windows animation effects are off (<see cref="SystemInformation.UIEffectsEnabled"/> or the
        /// Windows 10/11 "Animation effects" switch).
        /// Calling this again only updates the durations; it never subscribes twice. Do not combine it with
        /// <see cref="FadeIn"/>/<see cref="FadeOut"/> calls in Shown or FormClosing handlers.</para>
        /// <para>It does nothing at design time (for example in the constructor of a base form, which the Visual Studio designer runs
        /// when it opens a derived form), so the designed opacity is never changed or serialized.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="frm"/> is null.</exception>
        public static void EnableFade(this Form frm, int fadeInDuration, int fadeOutDuration)
        {
            if (frm is null)
            {
                throw new ArgumentNullException(nameof(frm));
            }
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
            {
                // a base form opened in the designer: never change the designed opacity (it could be serialized into a derived form)
                return;
            }
            if (_fades.TryGetValue(frm, out var state))
            {
                // already enabled: only the durations change
                state.InDuration = fadeInDuration;
                state.OutDuration = fadeOutDuration;
                return;
            }
            state = new FadeState
            {
                InDuration = fadeInDuration,
                OutDuration = fadeOutDuration,
                Target = frm.Opacity > 0 ? frm.Opacity : 1d
            };
            _fades.Add(frm, state);
            frm.VisibleChanged += Fade_VisibleChanged;
            frm.FormClosing += Fade_FormClosing;
            frm.FormClosed += Fade_FormClosed;
            if (frm.IsDisposed)
            {
                return;
            }
            if (!frm.Visible)
            {
                // not shown yet: start transparent (the fade-in starts when the form is shown)
                if (CanFade(frm, fadeInDuration))
                {
                    frm.Opacity = 0;
                    state.IsTransparent = true;
                }
                else if (frm.Opacity <= 0)
                {
                    // designed transparent for FadeIn but nothing will fade it in: show it at its target opacity
                    state.IsTransparent = true;
                }
            }
            else if (frm.Opacity <= 0)
            {
                // already on screen but transparent (a form designed for FadeIn): fade it in now
                StartFadeIn(frm, state);
            }
        }

        /// <summary>
        /// Rounds the corners of the control (usually a borderless form) with a round-rectangle region.
        /// </summary>
        /// <param name="ctrl">Control or form to shape.</param>
        /// <param name="corner">Width and height of the corner ellipse in pixels; 0 or less removes the region.</param>
        /// <remarks>
        /// Replaces <c>Region = Region.FromHrgn(CreateRoundRectRgn(...))</c>, which leaks a GDI region handle on every call: the native
        /// handle is released here and the previous region of the control is disposed. The region is built for the current size, so call
        /// it again after the control is resized.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="ctrl"/> is null.</exception>
        public static void SetRoundRegion(this System.Windows.Forms.Control ctrl, int corner)
        {
            if (ctrl is null)
            {
                throw new ArgumentNullException(nameof(ctrl));
            }
            YANShape.SetRoundRegion(ctrl, corner);
        }

        /// <summary>
        /// Highlight label link bằng tên control (prefix tên label bắt buộc là "lbl").
        /// </summary>
        /// <param name="ctrl">Control liên kết với label.</param>
        /// <param name="typeName">Loại control.</param>
        /// <param name="color">Màu highlight.</param>
        /// <param name="isBold">In đậm hoặc không.</param>
        /// <remarks>
        /// Does nothing when the control is not on a form, its name is shorter than <paramref name="typeName"/>, or no <see cref="Label"/> with that name exists.
        /// The label font becomes exactly Bold or Regular, as in 1.0.1; a Font is created only when the style changes.
        /// </remarks>
        public static void HighLightLblLinkByCtrl(this System.Windows.Forms.Control ctrl, string typeName, Color color, bool isBold)
        {
            if (ctrl is not { Name: { } name } || typeName is null || name.Length < typeName.Length || ctrl.FindForm() is not { } frm)
            {
                return;
            }
            if (frm.Controls.Find($"lbl{name.Substring(typeName.Length)}", true).OfType<Label>().FirstOrDefault() is not { } lbl)
            {
                return;
            }
            lbl.ForeColor = color;
            SetBold(lbl, isBold);
        }

        // Set the label font to exactly Bold or Regular (the 1.0.1 result), creating a Font only when the style actually changes
        private static void SetBold(Label lbl, bool isBold)
        {
            var font = lbl.Font;
            var style = isBold ? Bold : Regular;
            if (font.Style == style)
            {
                return;
            }
            var newFont = new Font(font, style);
            lbl.Font = newFont;
            // dispose the replaced font only if this helper created it for this label (never a consumer's or an inherited font)
            if (_highLightFonts.TryGetValue(lbl, out var created) && ReferenceEquals(created, font) && ReferenceEquals(lbl.Font, newFont))
            {
                font.Dispose();
            }
            _highLightFonts.Remove(lbl);
            _highLightFonts.Add(lbl, newFont);
        }

        // Device pixels at the control's DPI for a length designed at 96 dpi: the shared helper of the controls (YANPaint: the rounding of
        // Control.LogicalToDeviceUnits, the same length at 96 dpi, in a DPI-unaware process and on a WinForms without DeviceDpi; tests set
        // YANPaint.DpiOverride)
        internal static int LogicalToDevice(this System.Windows.Forms.Control ctrl, int value) => YANPaint.LogicalToDevice(ctrl, value);

        // Scale a form designed at 96 dpi (AutoScaleMode.Dpi) to its DPI now, before code sizes or places it: WinForms would otherwise
        // scale it at its next layout, over the sizes and bounds set meanwhile. Nothing changes at 96 dpi or in a DPI-unaware process
        internal static void ScaleToDpi(this ContainerControl ctrl)
        {
            ctrl.PerformAutoScale();
            if (YANPaint.DpiOverride is { } dpi)
            {
                // tests: from the machine's DPI (just applied) to the requested one
                var factor = dpi / ctrl.CurrentAutoScaleDimensions.Width;
                ctrl.Scale(new SizeF(factor, factor));
            }
        }

        // Time-based ease-out fade to an already clamped opacity; stops when the form is disposed or the token is cancelled
        private static async Task FadeAsync(Form frm, double opacity, int duration, CancellationToken token)
        {
            if (!IsAlive(frm) || token.IsCancellationRequested)
            {
                return;
            }
            var from = frm.Opacity;
            if (duration <= 0 || !IsAnimated || from == opacity)
            {
                frm.Opacity = opacity;
                return;
            }
            var clock = Stopwatch.StartNew();
            while (true)
            {
                var t = Min(1d, clock.Elapsed.TotalMilliseconds / duration);
                // ease-out cubic; the last frame sets the exact target
                frm.Opacity = t < 1d ? from + (opacity - from) * (1d - Pow(1d - t, 3)) : opacity;
                if (t >= 1d)
                {
                    return;
                }
                // yields to the message loop between frames
                await Task.Delay(FADE_FRAME_MS);
                if (!IsAlive(frm) || token.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        // Form still usable for Opacity changes
        private static bool IsAlive(Form frm) => !frm.IsDisposed && !frm.Disposing;

        // Opacity only has an effect on top-level windows (not on MDI children) and only when animations are on
        private static bool CanFade(Form frm, int duration) => duration > 0 && frm.TopLevel && IsAnimated;

        // Start (or restart) the EnableFade fade-in from opacity 0; without a fade-in, just show the form at its target opacity
        private static async void StartFadeIn(Form frm, FadeState state)
        {
            var token = state.Restart();
            state.IsTransparent = false;
            if (!CanFade(frm, state.InDuration))
            {
                frm.Opacity = state.Target;
                return;
            }
            frm.Opacity = 0;
            await FadeAsync(frm, state.Target, state.InDuration, token);
        }

        // EnableFade: fade in each time the form is shown (before it is painted: WinForms raises VisibleChanged from WM_SHOWWINDOW)
        private static void Fade_VisibleChanged(object sender, EventArgs e)
        {
            if (sender is not Form frm || !_fades.TryGetValue(frm, out var state) || !IsAlive(frm))
            {
                return;
            }
            if (!frm.Visible)
            {
                // hidden (or a modal form closed) during a fade: stop animating a window nobody sees, and restore it on the next show
                state.Restart();
                if (frm.Opacity != state.Target)
                {
                    state.IsTransparent = true;
                }
                return;
            }
            // a new showing (a modal form can be shown again before the previous close has finished its bookkeeping)
            state.Session++;
            state.IsClosed = false;
            state.IsFadingOut = false;
            state.IsReplaying = false;
            // run after the FormClosing handlers added so far (for example in Load), so their Cancel is seen before the fade starts
            frm.FormClosing -= Fade_FormClosing;
            frm.FormClosing += Fade_FormClosing;
            if (CanFade(frm, state.InDuration) || state.IsTransparent)
            {
                StartFadeIn(frm, state);
            }
        }

        // EnableFade: postpone an approved close until the form has faded out, then close it again
        private static async void Fade_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (sender is not Form frm || !_fades.TryGetValue(frm, out var state))
            {
                return;
            }
            if (state.IsReplaying)
            {
                // the postponed close: let it through
                state.IsReplaying = false;
                if (frm.Modal && frm.IsHandleCreated)
                {
                    // a modal form stays open when a handler cancels this close: check once all the handlers have run. This runs
                    // from the form's own message, which the modal loop only dispatches while the dialog is still open
                    var showing = state.Session;
                    frm.BeginInvoke(new Action(() =>
                    {
                        if (showing == state.Session && IsAlive(frm) && !state.IsClosed && !state.IsFadingOut && frm.Visible)
                        {
                            StartFadeIn(frm, state);
                        }
                    }));
                }
                return;
            }
            var isFadeReason = e.CloseReason is CloseReason.UserClosing or CloseReason.None;
            if (state.IsFadingOut)
            {
                // a second close request while fading out: the pending close covers it (an exit or shutdown still goes through)
                if (isFadeReason)
                {
                    e.Cancel = true;
                }
                return;
            }
            // (MDI children and owned forms get their own FormClosing from this close: a second close would raise it twice)
            if (e.Cancel || !isFadeReason || !CanFade(frm, state.OutDuration) || !frm.Visible || frm.Opacity <= 0 || !IsAlive(frm) || frm.MdiChildren.Length > 0 || frm.OwnedForms.Length > 0)
            {
                return;
            }
            // WinForms resets the DialogResult of a modal form when its close is cancelled: keep it for the second close
            var result = frm.DialogResult;
            var isModal = frm.Modal;
            var session = state.Session;
            e.Cancel = true;
            state.IsFadingOut = true;
            try
            {
                var fade = FadeAsync(frm, 0, state.OutDuration, state.Restart());
                if (fade.IsCompleted)
                {
                    // never close again from inside this FormClosing call
                    await Task.Yield();
                }
                await fade;
            }
            finally
            {
                // (also after a failure: a form stuck in this state could never be closed by the user)
                if (session == state.Session)
                {
                    state.IsFadingOut = false;
                }
            }
            if (session != state.Session)
            {
                // shown again meanwhile: that showing has its own fade-in
                return;
            }
            // from here the form stays transparent unless it is shown again or the second close is cancelled
            state.IsTransparent = true;
            // unless the form was closed, disposed or taken out of its modal loop another way meanwhile
            if (!IsAlive(frm) || state.IsClosed || isModal != frm.Modal)
            {
                return;
            }
            state.IsReplaying = true;
            if (isModal)
            {
                // the modal loop raises FormClosing again when it next checks the result: on .NET Framework after the current
                // message (the setter only stores the value), on mono inside the setter. IsReplaying stays set until then
                frm.DialogResult = result == DialogResult.None ? DialogResult.Cancel : result;
                return;
            }
            try
            {
                // WM_CLOSE is sent synchronously: the second FormClosing (and FormClosed) are raised inside Close
                frm.Close();
            }
            finally
            {
                if (session == state.Session)
                {
                    state.IsReplaying = false;
                }
            }
            if (session == state.Session && IsAlive(frm) && !state.IsClosed && frm.Visible)
            {
                // a FormClosing handler cancelled the second close: make the form visible again
                StartFadeIn(frm, state);
            }
        }

        // EnableFade: the form is closed (a modal form may be shown again later)
        private static void Fade_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (sender is Form frm && _fades.TryGetValue(frm, out var state))
            {
                state.IsClosed = true;
            }
        }
        #endregion

        #region Nested types
        // EnableFade settings and progress of one form
        private sealed class FadeState
        {
            private CancellationTokenSource _cts;

            public int InDuration { get; set; }

            public int OutDuration { get; set; }

            // Opacity of the fully shown form
            public double Target { get; set; }

            // Incremented each time the form is shown
            public int Session { get; set; }

            public bool IsFadingOut { get; set; }

            // Set while the postponed close runs (its FormClosing must go through)
            public bool IsReplaying { get; set; }

            public bool IsClosed { get; set; }

            // Made transparent by EnableFade (before the first show, or faded out for a close): restored when the form is shown
            public bool IsTransparent { get; set; }

            // Cancels the running fade of this form and returns the token of the next one
            public CancellationToken Restart()
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                return _cts.Token;
            }
        }
        #endregion
    }
}