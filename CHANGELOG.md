# Changelog

All notable changes to the [Tynab.YANF](https://www.nuget.org/packages/Tynab.YANF) package are recorded here. The format
follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow the policy below
([Semantic Versioning](https://semver.org/)).

## Versioning policy

YANF is used from C# and Visual Basic, mostly through the Windows Forms designer, which writes property values into the
consumer's `*.Designer.cs` / `*.Designer.vb` files. Public member signatures and designer-serialized properties (their
names, types and default values) are therefore the compatibility contract.

- **1.0.x (patch)**: bug fixes only. No public API is added, removed, renamed or changed, and behaviour changes only
  where the old behaviour was the bug.
- **1.x (minor)**: additive API only (new types, members and overloads). Existing members keep working. A member that a
  new API supersedes is hidden from IntelliSense with `[EditorBrowsable(EditorBrowsableState.Never)]` and keeps working.
  `[Obsolete]` is not used in 1.x: its warnings break consumers that build with warnings as errors, including their
  designer-generated code.
- **2.0 (major)**: breaking changes. Each one is marked **Breaking** in this file, with what to do instead.
- Every package build is checked against the last usable release, 1.0.1, by package validation. An accepted difference
  must be listed here and in `YANF/CompatibilitySuppressions.xml`.
- A `vX.Y.Z` tag builds, tests and publishes the package from CI; the tag must match the project version.

## [2.0.0] - Unreleased

The first release since 1.0.1 (1.0.2 is broken; the planned 1.1 additions ship here). It adds .NET 8 and .NET 10 builds,
a borderless base form (`YANForm`), an async loader (`YANLoader`), message box options, non-blocking fades and native
form dragging. The controls paint the parent's real background behind their rounded or transparent parts, with
anti-aliased edges (no more halo on gradients, #15), and scale with the DPI in DPI-aware apps. Designer files written
with 1.0.1 keep compiling and keep their look at 96 dpi, except where an entry below says otherwise.

### Breaking changes

- **Demo forms removed.** `YANF.MainFrm`, `YANF.Screen.Demo1` and `YANF.Screen.Demo2` are no longer in `YANF.dll`: they
  are the sample app [samples/YANF.Demo](https://github.com/Tynab/YANF/tree/main/samples/YANF.Demo). Affects code that
  created them (the 1.0 README suggested `new MainFrm().Show()`): drop the call or copy the forms from the sample.
- **`ic.ico` is no longer added to your project** (1.0.0 and 1.0.1 added it to packages.config projects). If yours uses
  it, copy it from `samples/YANF.Demo/ic.ico` before upgrading.
- **The screens are internal:** `YANLoadScreen`, `YANWaitScreen`, `YANUpdateScreen` and `YANMessageBoxScreen`, with
  their constructors, the fields `lblPercent`, `lblCapacity` and `pnlProgressBar`, `Frm_Close()` and `PrimaryColor`
  (`AnonScreen`, `SoftScreen`, `MiddleScreen` and `HardScreen` stay public). Affects code that created, showed, derived
  from or drove these forms itself: error CS0122, and in 1.x binaries `MissingMethodException`, `TypeLoadException` or
  `TypeAccessException`, `MethodAccessException`, `FieldAccessException`. Use `YANLoader` instead (`Show` or
  `RunWithLoaderAsync` with `YANLoaderOptions.Kind`; `YANLoaderScope.Report` or `SetProgress` for the fields, `Dispose`
  for `Frm_Close`), or the screen services (`YANLoadScrService`, `YANWaitScrService`, `YANUpdScrService`), and
  `YANMessageBox.Show` for message boxes. `PrimaryColor` has no replacement: the accent colour follows the icon.
- **`YANMessageBox` is a `static class`** (1.x: abstract, with a protected constructor). Every `Show` signature is
  unchanged, so calls keep working, also from 1.x binaries. Affects classes derived from it (CS0709; `TypeLoadException`
  in 1.x binaries) and code that uses it as a type (CS0718, CS0721 to CS0723): call `YANMessageBox.Show` directly, and
  turn a derived helper into your own static class.
- **`YANMessageBox.Show(null)` and `Show(owner, null)` no longer compile:** a literal `null` or `default` text is
  ambiguous between the `string` overloads and the new `YANMessageBoxOptions` ones (CS0121; BC30521 with `Nothing` in
  Visual Basic). Write `(string)null`, or pass a variable. 1.x binaries keep calling the `string` overloads.
- **`YANBtn.Focusable` is true by default, and YANBtn, YANTg, YANRdo and YANDp no longer force `TabStop = false`** (nor
  YANBtn `TabIndex = 0`). A 1.0.1 button never took the focus; now a click moves the focus to it, so the focused control
  raises `Leave` and `Validating` first and a cancelled validation cancels the click, SPACE and ENTER click it, and it
  draws a focus cue. 1.0.1 designer files write `TabStop = false`, so TAB still skips their controls; controls created
  in code are tab stops, and a button may take the first focus of its form. Set `Focusable = false` for the 1.x
  behaviour, or `TabStop = false` to keep only TAB and the first focus away.
- **`YANNb.ValueChanged` passes the YANNb as sender** (1.x: the inner `NumericUpDown`). Handlers that cast `sender` to
  `NumericUpDown` (now `InvalidCastException`) or compare it must use the YANNb, which has `Value`.
- **`YANDdl.BackColor` overrides `Control.BackColor`** and is the surface colour for every caller. 1.x hid
  `Control.BackColor`, which held the border colour: through a `Control` reference (a theme loop such as
  `foreach (Control c in Controls) c.BackColor = …`) it set and returned the border colour, and `BackColorChanged`
  followed `BorderColor`. Use `BorderColor` for the border. Designer values keep their look, except a transparent or
  translucent `BackColor` (1.x showed `BorderColor` behind the text and arrow, 2.0 the parent) and a `BackgroundImage`
  (now drawn inside the border only).
- **`&` in `YANRdo.Text` is a mnemonic**, as in a `RadioButton`: not drawn, it underlines the next character (hidden
  until ALT when Windows hides keyboard cues). Write `&&` for a literal `&`, or set `UseMnemonic = false` to draw the
  text as 1.x did.
- **Designer defaults of `YANBtn.FlatStyle`, `YANBtn.FlatAppearance.BorderSize` and `YANCirPic.SizeMode`** (for the
  designer, Reset and `ShouldSerializeValue`) are the constructor's values (`Flat`, 0, `StretchImage`), not the
  framework's (`Standard`, 1, `Normal`): the designer stops writing Flat, 0 and StretchImage and keeps Standard, 1 and
  Normal, which 1.x lost at run time. Designer files and 1.x binaries (bound to the base properties) keep working; only
  custom serializers or tools that assume the framework defaults are affected.
- **Rounded controls are painted, not cut with a region.** YANBtn (every `FlatStyle` but `System`), YANCirPic (with
  `BorderStyle` None), YANTxt and YANNb paint the parent around their shape, so their `Region` is null. As with a
  transparent `BackColor` in WinForms, sibling controls that overlap them no longer show through the corners: the parent
  is painted there. Clicks on the corners still reach what lies below, and a region set by your code is kept. Affects
  code that reads their `Region` and layouts where a round picture or button overlaps another control: avoid the
  overlap, or draw that content in the parent's Paint handler.
- **The parent's Paint event is raised more often:** also when YANTg, YANPrg, YANRdo, YANBtn, YANCirPic, a rounded or
  non-opaque YANTxt or YANNb, or a YANDdl or YANDp with non-opaque colours repaints (every YANPrg value, about 10 frames
  per YANTg slide, YANBtn hover and press), with `ClipRectangle` set to that control's area. Keep Paint handlers free of
  side effects and honour `ClipRectangle`.
- **`YANMath.Min` and `Max` compare with `Comparer<T>.Default`** (1.x: the `<` and `>` operators at run time through
  `dynamic`), so a type with `<` and `>` operators but no `IComparable` throws `ArgumentException`: implement
  `IComparable<T>` on it. Null and NaN arguments give the same results as in 1.x.

### Added

- **.NET builds:** `lib/net8.0-windows7.0` (used by .NET 8 and 9 apps) and `lib/net10.0-windows7.0` (.NET 10 and later)
  next to `lib/net481`, with the same public API. .NET 6 and 7 apps still get `lib/net481` (warning NU1701).
- **`YANF.Screen.YANForm`**, a base form for custom title bars, borderless by default (`FormBorderStyle` None, not
  written to the designer file): `RoundedCorners` (true; rounded by Windows 11, with its shadow, or by a region of
  `CornerRadius`, 8, before), `DropShadow` (true; `CS_DROPSHADOW` where Windows 11 does not round the window),
  `Resizable` and `ResizeBorderWidth` (true, 6), `CaptionHeight` (0) and `RegisterCaptionControl` /
  `UnregisterCaptionControl` (drag to move, double-click to maximize or restore). The taskbar button minimizes and
  restores it; it maximizes to the working area. Sizes are 96-dpi pixels scaled per monitor. Protected: `CreateParams`,
  `WndProc`, `OnHandleCreated`, `OnHandleDestroyed`, `OnStyleChanged`, `OnSizeChanged`, `Dispose(bool)`.
- **`YANLoader`** (`YANF.Script`) shows a Load, Wait or Update screen over a form, on its UI thread, while async work
  runs: `Show(owner)`, `Show(owner, options)` and `RunWithLoaderAsync(this Form, work)`, with overloads for options and
  a `CancellationToken` and for work that returns a result. The owned screen appears only after `ShowDelay`, follows the
  form (an MDI child or embedded form also when its parents move) and never covers a dialog the work opened; the form
  takes no input meanwhile (no clicks, keys or wheel turns, also before the screen appears) and always gets it back,
  whatever happens: with several loaders on one form it gets it back when the last one closes, and not while a modal
  dialog opened meanwhile is still open. The user cannot close the screen (Alt+F4). `YANLoaderScope` (`IDisposable`, `IProgress<int>`, also the progress given to the work): `Report(int)` and
  `SetProgress(int percent, string detail)` from any thread, `Dispose()` on the UI thread. `YANLoaderOptions`: `Kind`
  (`YANLoaderKind.Load`, `Wait`, `Update`), `ShowDelay` (250 ms), `Corner` (0), `FadeDuration` (200 ms).
- **Message box options:** `YANMessageBoxOptions` (`Caption`, `Text`, `Buttons`, `Icon`, `DefaultButton`, `Language`
  (null: English), `TopMost` (true), `StrictClose` (false; true makes ✕ give the ESC result, or hides it when there is
  none)), `YANMessageBox.Show(options)` and `Show(owner, options)` (shown on the owner's UI thread),
  `YANConstant.MsgBoxLang.ENG` (= 2), `[Flags]` on `YANConstant.AnimateWindowFlags`.
- **Forms and helpers:** `YANDisplay.FadeToAsync(this Form, double opacity, int duration)` (ease-out, non-blocking,
  instant when Windows animation effects are off);
  `YANDisplay.EnableFade(this Form, int fadeInDuration, int fadeOutDuration)` (fades in when shown and out when closed,
  keeps a modal `DialogResult`, survives a cancelled close); `YANDisplay.SetRoundRegion(this Control, int corner)` (no
  GDI leak); `YANEvent.EnableDrag(this Control)` and `DisableDrag` (drag the form through Windows' own move loop);
  `YANController.GetAllObjs<T>(this Control)` and `GetAllObjs<T>(this Control, Func<Control, bool> descendInto)`
  (derived types included, lazy); `HardScreen.BlockAltF4` (default true); `YANString.TryParseDouble`, `TryParseInt`,
  `ParseDouble` and `ParseInt` with an `IFormatProvider`.
- **Controls:**
  - `YANTxt.Text` (the content: bindable, localizable, written by the designer), `TextChanged` (the default event),
    `ReadOnly`, `AcceptsReturn`, `UseSystemPasswordChar`.
  - `YANDdl.SelectedIndexChanged` (the default event), `SelectedValue` and `SelectedValueChanged` (`ComboBox` rules),
    `Text` (the combo box text, bindable, not written by the designer; `String` stays the prompt), `TextChanged`,
    `BorderFocusColor` and `IconFocusColor` (HotPink). The new YANTxt and YANDdl events pass the control as sender.
  - `YANNb.ReadOnly`; `YANBtn.Focusable` and `YANBtn.PerformClick()`, which clicks whatever `Focusable` is (YANBtn
    implements `IButtonControl` again, so `AcceptButton`, `CancelButton` and `&` mnemonics work).
  - YANTg slides its knob in about 150 ms (ease-out, blended colours). It jumps when Windows animation effects are off
    (`UIEffectsEnabled`, or Settings > Accessibility > Visual effects > Animation effects) or the toggle cannot be seen.
  - Focus cues on YANBtn, YANTg, YANRdo and YANDp; `AccessibleName` and `AccessibleDescription` of YANTxt, YANNb and
    YANDdl reach their inner control; in high contrast mode YANTg, YANRdo, YANPrg and YANDp paint with system colours,
    and YANBtn, YANCirPic, YANTxt, YANNb and YANDdl draw their borders in them (YANBtn also its focus cue); their other
    colours are kept, and a transparent YANDdl `BorderColor` stays transparent.
  - `YANNb.OnValueChanged` and `YANDdl.OnSelectedValueChanged` (protected virtual; they raise the events), and protected
    overrides that derived controls extend: `Dispose(bool)`, `RescaleConstantsForDpi`, `OnPaintBackground`, `WndProc`,
    `OnParentBackColorChanged`, `OnCreateControl`, `DefaultCursor`, `OnSizeChanged`, `OnHandleCreated`, YANBtn's
    `ProcessMnemonic` and `ShowFocusCues`, YANDp's `OnFontChanged`, `OnFormatChanged`, `OnGotFocus`, `OnLostFocus` and
    `OnValueChanged`, YANTg's `OnCheckedChanged` and `OnVisibleChanged`.
- **Designer:** `[DefaultValue]` matching the constructor on every YANF property (only changed values are written, Reset
  works), toolbox icons, default events and properties, default binding properties (and lookup ones on YANDdl),
  localizable `YANTxt.PlaceholderText` and `YANDdl.String`.
- **Package and repository:** XML documentation, a symbols package with Source Link, a README and an icon; xUnit tests;
  a GitHub Actions workflow that builds, tests (net481, net10.0-windows), packs, validates against 1.0.1, checks the
  package contents and publishes only from `v*` tags.

### Changed

- **Painting:** YANTg, YANPrg, YANRdo, YANBtn, YANCirPic, YANTxt and YANNb, and YANDdl and YANDp where their colours are
  not opaque, show the parent's real background (gradient, `BackgroundImage`, Paint handler drawing) behind their
  transparent parts, with anti-aliased edges; 1.x painted `Parent.BackColor` there. YANGradPnl paints its gradient as
  its background, so its children show it.
- **DPI:** nothing changes at 96 dpi or in DPI-unaware apps (the .NET Framework default). In a DPI-aware app the
  controls' pixel sizes are 96-dpi units scaled to their DPI: `BorderSize` and `BorderRadius` of YANBtn, YANTxt and
  YANNb; `BorderSize` of YANCirPic, YANDdl (and the `Padding` it sets) and YANDp; `ChannelHeight` and `SliderHeight` of
  YANPrg; the drawing of YANTg and YANRdo, the YANDdl arrow and the YANDp icon. YANTxt and YANNb no longer force
  `AutoScaleMode.None`: they scale with their form and fit their height to the font again after a per-monitor DPI change
  (YANTxt keeps its text, selection and undo). The Load, Wait and Update screens and the message box use
  `AutoScaleMode.Dpi`; the message box also scales its margins, default-button border and icon, again on another
  monitor. `YANUpdScrService.PublishValue` reads `width` in 96-dpi pixels out of `YANConstant.W_UPDATE_SCR` and scales
  it with the screen.
- **Text:** YANRdo, the YANPrg value and the YANDp text are drawn with `TextRenderer` (GDI) instead of
  `Graphics.DrawString`, in the same place: the glyphs match the standard controls and differ slightly from 1.x.
- **Look at 96 dpi:** a rounded (`BorderRadius` > 1), not underlined YANTxt or YANNb draws its border over its outer
  `BorderSize` pixels; 1.x left about `BorderSize / 2` pixels outside it, so the border moves out by that much (1 px for
  YANTxt's default of 2), the surface grows as much and the outer radius is `BorderRadius`. Square and underlined boxes
  are unchanged. A rounded, not underlined YANNb with `BorderSize` 0 no longer draws a 1-px line at its top and left.
  Straight YANBtn and YANCirPic edges are crisp for even border sizes, and the YANBtn focus cue sits 3 px inside the
  border for odd sizes too.
- **Controls:** `YANTxt.Text` and `YANDdl.Text` are the content (1.x: the unused `Text` of `UserControl`). YANTxt
  suppresses ENTER only in a single-line box without `AcceptsReturn`. A focused YANDdl draws its border and arrow in
  HotPink, unless `BorderColor` or `IconColor` is fully transparent (a border or arrow that a 1.x designer file hid that
  way stays hidden); `Color.Empty` in `BorderFocusColor` and `IconFocusColor` turns it off. YANDdl's `AutoCompleteMode`,
  `AutoCompleteSource` and `DropDownStyle` defaults match the constructor, so a value such as `AutoCompleteMode.None` is
  now kept. Some property categories and descriptions are corrected.
- **Message box:** ENTER presses the default button and ESC presses Cancel (OK in an OK box), as in Windows; Yes/No and
  Abort/Retry/Ignore have no ESC button, and ✕ returns Cancel unless `StrictClose`. The default button has a white
  border instead of an underlined font; long messages wrap at about half the screen width; an owner on another UI thread
  shows the box on that thread; an invalid `MessageBoxButtons` throws `InvalidEnumArgumentException`, an invalid
  `MsgBoxLang` gives English buttons.
- **Helpers:** `YANMath.Min` / `Max` use `Comparer<T>.Default` (strings and any `IComparable` work; an empty or null
  array throws `ArgumentException` / `ArgumentNullException`; null and NaN behave as in 1.x). The hidden
  `MoveFrm_*` handlers react to the left button only and restore the form's previous opacity.
- **On .NET** the default font is Segoe UI 9pt, so YANBtn, YANTxt, YANNb, YANDdl, YANDp and YANRdo, which take their
  font family from it, use Segoe UI, as 1.0.1 did on .NET. For the .NET Framework look, call
  `Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25F))` before creating any window.
- **Package:** `YANF.dll` is about 1 MB instead of 2.3 MB (the demo images moved to the sample; unused images and the
  screens' window icons are gone). The project is SDK-style (C# 12), Source Link comes from the .NET SDK, and the sample
  and the tests also run on .NET 10.

### Fixed

- **Halo on gradients (#15):** no ring of `Parent.BackColor` around YANTg, YANPrg, YANRdo, YANBtn, YANCirPic, YANTxt,
  YANNb or a transparent YANDp on a YANGradPnl or any gradient or image parent, and no band over the progress under the
  sliding YANPrg value. YANGradPnl and YANCirPic no longer wrap the end colour onto the first row of their gradients.
- **.NET 9 and later:** apps that used the .NET Framework DLL could not load YANF's images (message box icons, screens,
  YANDp calendar), stored with BinaryFormatter, which .NET 9 removed. The .NET builds store them without it.
- **Screen services** (`YANLoadScrService`, `YANWaitScrService`, `YANUpdScrService`) are thread-safe: the screen runs on
  its own STA thread, `OnLoader` returns once it is up (or rethrows its failure), and `PublishValue` and `OffLoader`
  work from any thread, at any time and more than once (the latest value wins). The process no longer outlives the app,
  the update box is centred on its form, and region handles are freed. `MiddleScreen.Frm_Close()` closes the form (1.x
  threw `NotImplementedException`), and resized screens rebuild their rounded corners. The Load, Wait and Update
  screens dispose their animated GIF (over a megabyte of native memory each) instead of leaving it to the finalizer, and
  refuse a close by the user (Alt+F4), which left the form blocked without a screen.
- **Message box:** an OK box with `Button2` or `Button3` as default focused a hidden button; the ✕ handler ran twice;
  every box leaked six or seven fonts and a bitmap.
- **Controls:** no exceptions from `AddArc` or `LinearGradientBrush` at small sizes or with a border larger than the
  radius; resizing no longer shrinks `BorderRadius` / `BorderSize` for good (negative values are stored as 0); no region
  is built in `OnPaint` (an endless repaint loop); GDI objects are disposed. YANBtn works without a parent and keeps a
  `Cursor` set by the app. YANPrg keeps repainting after `Maximum` and handles `Minimum == Maximum`. YANDp paints into
  the paint event (printing and `DrawToBitmap` work). YANRdo's hover no longer changes `ForeColor`. YANCirPic stays
  square unless docked, and with a `BorderStyle` its region matches the circle. YANNb accepts negative ranges in
  designer order, and `String` follows the value. YANTxt draws its placeholder instead of writing it into the text.
  YANDdl: `DropDownStyle` tested the old value; clearing the text or the selection shows the prompt again; `TextAlign`
  applies at once. `BorderFocusColor` of YANTxt and YANNb repaints at once.
- **YANPrg without visual styles:** the channel, the slider and the value are laid out in the client area, so the
  1-pixel frame Windows gives an unthemed progress bar no longer cuts the bars at the bottom and the value at the right.
  Apps with visual styles look the same.
- **Helpers:** `HighLightLblLinkByCtrl` leaked a `Font` on every call, and threw for a control that is not on a form,
  whose name is shorter than `typeName`, or when the first control with the label's name is not a `Label` (it now looks
  for a `Label` with that name); as in 1.0.1, the label becomes exactly Bold or Regular;
  `YANMath` threw `RuntimeBinderException` for strings; `MoveFrm_*` left the form dimmed and following the mouse when
  the MouseUp was lost.

### Deprecated (hidden)

Hidden from IntelliSense with `[EditorBrowsable(EditorBrowsableState.Never)]` (the YANTxt properties and the events also
from the property grid), not `[Obsolete]`: existing code, designer files and binaries keep working. `FadeIn` / `FadeOut`
(they block the UI thread while they fade) and the screen services (the choice for work that blocks the UI thread, since
they run the screen on its own thread) are not deprecated.

- `YANTxt.String`: use `Text` (the designer writes `Text` on the next save; `String` is null while the placeholder
  shows).
- `YANTxt.StringChanged` and `YANDdl.StringChanged`: use `TextChanged`; `YANDdl.OnSelectedIndexChanged` (an event): use
  `SelectedIndexChanged`. They are still raised, with the inner control as sender.
- `YANTxt.PasswordChar`: use `UseSystemPasswordChar`. `YANEvent.MoveFrm_MouseDown`, `MoveFrm_MouseMove` and
  `MoveFrm_MouseUp`: call `EnableDrag` once. `YANDisplay.AnimateWindow`: use `FadeToAsync` or `EnableFade`.
  `YANDisplay.CreateRoundRectRgn`: use `SetRoundRegion` (`Region.FromHrgn(CreateRoundRectRgn(…))` leaks a GDI handle).

### Known issues

- `YANForm`: Windows does not apply Aero Snap to a borderless window (nor, with `EnableDrag`, to any form without a
  sizable border); docked child controls cover the resize edges (keep them free with `Padding`); whether Windows 11
  rounds and shadows a translucent (layered) window is not verified on every build.

### Upgrade guide (1.0.1 to 2.0)

1. **Package:** update to 2.0.0 (never 1.0.2). In a packages.config project that uses `ic.ico`, copy it first. Remove
   any use of `MainFrm`, `Demo1` or `Demo2`.
2. **Designer files** need no edits; the designer rewrites them on the next save (lines at their default, such as
   `FlatStyle = Flat`, go, and `YANTxt.String` becomes `Text`). Build: expect CS0122 on the screen types, CS0709 or
   CS0718 to CS0723 on `YANMessageBox`, and CS0121 on `YANMessageBox.Show(null)` and `Show(owner, null)`.
3. **Screens now internal:** replace the screen forms and their fields with `YANLoader`
   (`await this.RunWithLoaderAsync(work)`, or `using var scope = YANLoader.Show(this, options)` with
   `scope.SetProgress(percent, detail)`) or keep the screen services; show message boxes with
   `YANMessageBox.Show(owner, options)`.
4. **Message box static:** stop deriving from `YANMessageBox` or using it as a type; call `YANMessageBox.Show`. Write a
   literal null text as `(string)null`.
5. **String → Text:** use `YANTxt.Text` and `TextChanged`. For YANDdl, `Text` and `TextChanged` are the content and
   `SelectedIndexChanged` replaces `OnSelectedIndexChanged`; `YANDdl.String` stays the prompt.
6. **Event senders:** the new events and `YANNb.ValueChanged` pass the YAN control: fix handlers that cast `sender` to
   `NumericUpDown`. The hidden legacy events keep the inner control.
7. **TabStop and Focusable:** every YANBtn takes the focus now: set `Focusable = false` where that matters (next to a
   control whose `Validating` cancels, for example). To reach an existing YANBtn, YANTg, YANRdo or YANDp with TAB, set
   `TabStop = true`.
8. **Look:** check `&` in YANRdo texts, YANDdl colours set through `Control.BackColor`, round controls that overlap
   others, rounded YANTxt and YANNb borders (moved out by half their size) and the parent's Paint handlers.
9. **DPI-aware hosts:** at 96 dpi and in DPI-unaware apps nothing is scaled. In a DPI-aware app (see "High DPI" in the
   README) the YANF controls, screens and message box scale: design your forms at 96 dpi with `AutoScaleMode.Dpi` (or
   `Font`) and check them at 150 % and on a monitor with another scale.

## [1.0.2] - 2023-05-24

### Security

- **Broken package, do not use.** It has no `YANF.dll` under `lib/`, so it adds no reference, and it ships build output
  and tooling. Use 1.0.1 or a later version.

## [1.0.1] - 2022-10-30

- No release notes were recorded.

## [1.0.0] - 2022-10-20

- First release.

[2.0.0]: https://github.com/Tynab/YANF/commits/main
[1.0.2]: https://www.nuget.org/packages/Tynab.YANF/1.0.2
[1.0.1]: https://www.nuget.org/packages/Tynab.YANF/1.0.1
[1.0.0]: https://www.nuget.org/packages/Tynab.YANF/1.0.0
