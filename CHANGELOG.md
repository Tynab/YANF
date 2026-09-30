# Changelog

All notable changes to the [Tynab.YANF](https://www.nuget.org/packages/Tynab.YANF) package are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and versions follow the policy below
([Semantic Versioning](https://semver.org/)).

## Versioning policy

YANF is used from C# and Visual Basic, mostly through the Windows Forms designer, which writes property values into the
consumer's `*.Designer.cs` / `*.Designer.vb` files. Public member signatures and designer-serialized properties (their
names, types and default values) are therefore the compatibility contract.

- **1.0.x (patch)**: bug fixes only. No public API is added, removed, renamed or changed, and behaviour changes only
  where the old behaviour was the bug.
- **1.x (minor)**: additive API only (new types, members and overloads). Existing members keep working. A member that
  a new API supersedes is hidden from IntelliSense with `[EditorBrowsable(EditorBrowsableState.Never)]` and keeps
  working. `[Obsolete]` is not used in 1.x: its warnings break consumers that build with warnings as errors, including
  their designer-generated code.
- **2.0 (major)**: breaking changes. Each one is marked **Breaking** in this file, with what to do instead.
- Every package build is checked against the last usable release, 1.0.1, by package validation. An accepted
  difference must be listed here and in `YANF/CompatibilitySuppressions.xml`.
- A `vX.Y.Z` tag builds, tests and publishes the package from CI; the tag must match the project version.

## [Unreleased]

The removal of the demo forms below is a breaking change, so under this policy the release is a major version: the
project currently builds `2.0.0-alpha.1`.

### Security

- **Do not use 1.0.2.** The 1.0.2 package on nuget.org has no `YANF.dll` under `lib/`, so it adds no reference and
  installs nothing usable, and it carries build output (`bin/`, `obj/`) and tooling that do not belong in a package. Use
  1.0.1 or this release.
- CI now refuses a package that lacks `lib/net481/YANF.dll` or contains a `nuget.config`, executables, `bin/`, `obj/`
  or `content/` folders or source files, and it publishes only from `v*` tags.

### Removed

- **Breaking:** `YANF.MainFrm`, `YANF.Screen.Demo1` and `YANF.Screen.Demo2` are no longer in `YANF.dll`. They were the
  demo forms of the library, not part of its API, and now live in the sample application
  [samples/YANF.Demo](https://github.com/Tynab/YANF/tree/main/samples/YANF.Demo) (namespace `YANF.Demo`). Code that
  created them (the README used to suggest `new MainFrm().Show()`) must drop that call or copy the forms from the
  sample; to see the demo, open `YANF.sln` and run `YANF.Demo`.
- The demo's entry point (`Program.Main`, internal) is no longer in the library.
- **Breaking (packages.config projects):** the package no longer adds `ic.ico` to your project (1.0.0 and 1.0.1 did). If
  your project uses it, copy it from `samples/YANF.Demo/ic.ico` before upgrading.
- `YANF.dll` no longer embeds the 25 images that only the demo used or that nothing used: the 13 demo images moved to
  the sample, the 12 unused ones (`pPE`, `pPF`, `pPS`, `pPTLT`, `pPTTBH`, `pPTTCB`, `pPTTCV`, `pPTTKN`, `pPTTMR`,
  `pPU`, `pQL`, `pUserSolid1`) are deleted. `YANF.Properties.Resources` is internal, so this is not an API change.
- The Load, Wait, Update and message box screens no longer embed a window icon, which they never showed (about 640 KB
  less).
- The `Microsoft.CSharp` reference (`YANMath` no longer uses `dynamic`).

### Added

- `YANString` overloads with an explicit culture: `TryParseDouble(IFormatProvider, out double)`,
  `TryParseInt(IFormatProvider, out int)`, `ParseDouble(IFormatProvider, double fallback)` and
  `ParseInt(IFormatProvider, int fallback)`. The existing `ParseDouble()` / `ParseInt()` are unchanged (current culture,
  0 on failure).
- The package ships the XML documentation file, a symbols package (`.snupkg`) with Source Link, a README and an icon.
- An xUnit test project (`tests/YANF.Tests`) covering the controls, the helpers, the screen services and the demo.
- A GitHub Actions workflow: build with Visual Studio MSBuild on Windows, tests, pack, package validation against 1.0.1,
  a package content check, and publishing to nuget.org from `v*` tags.

Loader (namespace `YANF.Script`): the recommended way to show a Load, Wait or Update screen while async work runs

- `YANLoader`: static class that shows a screen over a form, on the form's own UI thread. The screen is an owned,
  non-top-most window that appears only when the work is still running after `ShowDelay`, follows the form when it is
  moved or resized, hides while it is minimized or hidden, and never covers a message box or dialog that the work opened
  before the screen appeared. Meanwhile the form takes no mouse or keyboard input (its controls are not greyed out), and
  it always gets its input back: after success, an exception, a cancellation, or the form closing during the work.
- `YANLoader.Show(Form owner)`: shows a Load screen and returns its `YANLoaderScope`. Call it on the owner's UI thread.
- `YANLoader.Show(Form owner, YANLoaderOptions options)`: the same with options (null uses the defaults).
- `YANLoader.RunWithLoaderAsync(this Form owner, Func<IProgress<int>, CancellationToken, Task> work)`: runs the work
  with a Load screen. The returned task ends once the screen is gone, with the work's exception or cancellation.
- `YANLoader.RunWithLoaderAsync(this Form owner, Func<IProgress<int>, CancellationToken, Task> work, YANLoaderOptions
  options, CancellationToken cancellationToken)`: the same with options, and a token passed to the work (an already
  cancelled token shows nothing and returns a cancelled task).
- `YANLoader.RunWithLoaderAsync<T>(this Form owner, Func<IProgress<int>, CancellationToken, Task<T>> work)`: the same
  for work that returns a result.
- `YANLoader.RunWithLoaderAsync<T>(this Form owner, Func<IProgress<int>, CancellationToken, Task<T>> work,
  YANLoaderOptions options, CancellationToken cancellationToken)`: the same with options and a token.
- `YANLoaderScope` (`IDisposable`, `IProgress<int>`): an open screen. It is also the progress object that
  `RunWithLoaderAsync` passes to the work: cast it to call `SetProgress`.
- `YANLoaderScope.Report(int value)`: shows a percentage (clamped to 0-100) and keeps the detail text. Safe from any
  thread; when values arrive faster than the screen can show them, only the latest is shown.
- `YANLoaderScope.SetProgress(int percent, string detail)`: shows a percentage and a detail text (the Update screen
  shows the detail, for example "68 MB / 137 MB"). Safe from any thread.
- `YANLoaderScope.Dispose()`: gives the owner its input back at once, then fades the screen out and closes it; calling
  it again does nothing. Call it on the UI thread: another thread gets `InvalidOperationException`, and the scope is
  still closed on the UI thread.
- `YANLoaderOptions`: sealed class, read when the screen is created:
  - `Kind`: the screen to show. Default `YANLoaderKind.Load`.
  - `ShowDelay`: milliseconds before the screen appears, so that short work shows nothing. Default 250; 0 or less shows
    it at once.
  - `Corner`: corner size of the screen's rounded region in pixels. Default 0 (square corners).
  - `FadeDuration`: fade-in and fade-out in milliseconds. Default 200; 0 or less means no fade.
- `YANLoaderKind`: `Load` (percentage, covers the owner), `Wait` (no progress, covers the owner) and `Update` (a box
  centred on the owner with a percentage, a detail text and a progress bar).
- `YANF.Screen.YANOverlayScreen`: the public base class of `YANLoadScreen`, `YANWaitScreen` and `YANUpdateScreen`
  (it derives from `MiddleScreen`), with a parameterless constructor. It rebuilds its rounded corners when resized.
- `YANOverlayScreen.SetProgress(int percent, string detail)` (protected virtual): shows progress. The base shows nothing
  (the Wait screen); `YANLoadScreen` overrides it to show the percentage and `YANUpdateScreen` to show the percentage,
  the detail text and a bar sized from the screen's own width.
- `YANOverlayScreen.ComputeBounds(Rectangle ownerBounds)` (protected virtual): the screen's bounds. The base covers the
  owner; `YANUpdateScreen` overrides it to centre the box on the owner, inside the working area.
- `YANOverlayScreen` overrides `Frm_Close()` (fades out, closes and disposes, as the 1.0 screens did), `OnShown`,
  `OnSizeChanged`, `OnFormClosed` and `Dispose(bool)`.

Message box

- `YANMessageBoxOptions`: sealed class with everything a message box shows, read when the box is created. The defaults
  give the same box as `YANMessageBox.Show(text)`:
  - `Caption`: the title. Default null.
  - `Text`: the message.
  - `Buttons`: default `MessageBoxButtons.OK`.
  - `Icon`: also sets the accent colour. Default `MessageBoxIcon.None`.
  - `DefaultButton`: the button that has the focus and that ENTER presses. Default `Button1`; a button the box does not
    show falls back to the first one.
  - `Language` (`MsgBoxLang?`): button texts and fonts. Default null, the English texts and fonts of the overloads
    without a language.
  - `TopMost`: default true, like every 1.0 message box.
  - `StrictClose`: when true, the close box (✕) gives the result of the button that ESC presses and is hidden when there
    is none (Yes/No, Abort/Retry/Ignore). Default false: ✕ gives `DialogResult.Cancel`, as in 1.0.
- `YANMessageBox.Show(YANMessageBoxOptions options)`: shows the box without an explicit owner (WinForms makes the active
  window of the calling thread its owner, as the overloads without an owner always did).
- `YANMessageBox.Show(IWin32Window owner, YANMessageBoxOptions options)`: shows the box modal to `owner`. When `owner`
  is a control of another UI thread, the box is shown on that thread and the call waits for it. The 20 existing `Show`
  overloads forward to it and give the same box as before.
- `YANMessageBoxScreen(YANMessageBoxOptions options)`: constructor of the message box form (`YANMessageBox.Show`
  creates, shows and disposes the form for you).
- `YANConstant.MsgBoxLang.ENG` (= 2): English button texts and fonts, the same as a box without a language. `VIE` (0)
  and `JAP` (1) keep their values and texts.
- `[Flags]` on `YANConstant.AnimateWindowFlags`, whose values are combined.
- Source note: `YANMessageBox.Show(null)` and `YANMessageBox.Show(owner, null)` with a literal `null` (`Nothing` in VB)
  are now ambiguous between the string and the options overloads; write `(string)null` to keep the 1.0 overload.
  Compiled code is not affected.

Forms and helpers

- `YANDisplay.FadeToAsync(this Form frm, double opacity, int duration)`: fades the form to `opacity` (clamped to 0-1;
  NaN throws `ArgumentOutOfRangeException`) in `duration` milliseconds with an ease-out curve, without blocking the UI
  thread. The target is applied at once when the duration is 0 or less or Windows animation effects are off; a form
  disposed before or during the fade is left alone.
- `YANDisplay.EnableFade(this Form frm, int fadeInDuration, int fadeOutDuration)`: fades the form in each time it is
  shown (it starts transparent, so it never flashes) and out before it closes, without blocking the UI thread. A modal
  form keeps its `DialogResult`, and a form whose second close is cancelled by a `FormClosing` handler fades back in.
  There is no fade when the close is already cancelled; for Application.Exit, Windows shutdown, the Task Manager or the
  owner or MDI parent closing; for MDI children; and while MDI children or owned forms are open. Calling it again only
  updates the durations. It does nothing at design time.
- `YANDisplay.SetRoundRegion(this Control ctrl, int corner)`: rounds the corners of a control or borderless form
  without leaking the GDI region handle, and disposes the previous region; 0 or less removes the region.
- `YANEvent.EnableDrag(this Control handle)`: pressing the left mouse button on the control drags its form through
  Windows' own move loop, so Aero Snap, moves across monitors and ESC work. Double-clicks and the other buttons reach
  the control as usual; a press that drags the form gives the control no MouseUp or Click. Calling it twice has no extra
  effect.
- `YANEvent.DisableDrag(this Control handle)`: undoes `EnableDrag`.
- `YANController.GetAllObjs<T>(this Control ctrl)`: every descendant that is a `T` or derives from it (for example
  `YANGradPnl` for `Panel`), depth-first in pre-order, without `ctrl` itself. The tree is walked lazily with an explicit
  stack. `GetAllObjs(Control, Type)` keeps its exact-type match.
- `YANController.GetAllObjs<T>(this Control ctrl, Func<Control, bool> descendInto)`: the same, walking only into the
  children of the controls for which `descendInto` returns true (for example to skip the inner label of a `YANDdl`).
- `HardScreen.BlockAltF4` (default true, the 1.0 behaviour): set it to false to let Alt+F4 close the form.

Controls

- `YANTxt.Text`: now the main property, the content of the box: bindable, localizable and written by the designer
  (default ""). A YANTxt newly dropped in the designer starts empty instead of showing its name.
- `YANTxt.TextChanged`: raised when `Text` changes, typed or set by code, with the YANTxt as sender; focus changes
  never raise it. It is the default event.
- `YANTxt.ReadOnly` (default false).
- `YANTxt.AcceptsReturn` (default false): ENTER types a new line in a multiline box, and is not suppressed in a
  single-line box.
- `YANTxt.UseSystemPasswordChar` (default false): masks the text; the same value as the legacy `PasswordChar`.
- `YANNb.ReadOnly` (default false).
- `YANNb.OnValueChanged(EventArgs)` (protected virtual): raises `ValueChanged`, still with the inner `NumericUpDown` as
  sender.
- `YANDdl.SelectedIndexChanged`: raised when the selected index changes, with the YANDdl as sender. It is the default
  event.
- `YANDdl.SelectedValue`: the `ValueMember` value of the selected item, bindable, with the rules of `ComboBox`.
- `YANDdl.SelectedValueChanged`: raised when `SelectedValue` changes, with the YANDdl as sender.
- `YANDdl.OnSelectedValueChanged(EventArgs)` (protected virtual): raises `SelectedValueChanged`.
- `YANDdl.Text`: the text of the combo box (the selected item or the typed text), bindable (the default binding
  property) and not written by the designer. `String` stays the prompt ("Select...").
- `YANDdl.TextChanged`: raised when that text changes, with the YANDdl as sender; focus changes never raise it.
- `YANDdl.BorderFocusColor` and `YANDdl.IconFocusColor` (default HotPink): the border and arrow colours while the
  control has the focus, without changing `BorderColor` or `IconColor`.
- `YANBtn.Focusable` (default false, the 1.0 behaviour): when true, the button takes the focus like a standard `Button`.
  A click moves the focus to it (so the previous control validates first), TAB reaches it when `TabStop` is true, SPACE
  and ENTER click it, and it draws a focus cue.
- `YANBtn.PerformClick()`: clicks the button whatever `Focusable` is. It hides `Button.PerformClick`, which does nothing
  while the button cannot take the focus; YANBtn implements `IButtonControl` again, so `Form.AcceptButton` and
  `Form.CancelButton` call the new method.
- Protected overrides: `YANBtn.ProcessMnemonic(char)` and `YANBtn.ShowFocusCues`; `YANDp.OnGotFocus` and
  `YANDp.OnLostFocus`; `OnCreateControl` on YANTxt, YANNb and YANDdl; `YANDdl.OnPaintBackground`.
- The `AccessibleName` and `AccessibleDescription` of YANTxt, YANNb and YANDdl are passed to their inner control, so
  screen readers announce them.
- Keyboard focus cues, shown once Windows shows focus cues: YANBtn when `Focusable` is true (inside its rounded shape),
  YANTg (a dotted line in the toggle colour), YANRdo (around the text, or the circle when there is no text) and YANDp
  (inside the border).
- High contrast mode: the YANBtn and YANDp borders and the YANCirPic ring use `SystemColors.WindowFrame`, and the focus
  cues use system colours. The configured property values do not change.

Designer

- `[DefaultValue]` matching the constructor on every designer property that YANF declares: the designer no longer
  writes unchanged values, only changed values show in bold, and Reset works. Existing `*.Designer.cs` files keep
  working; lines at their default disappear the next time the form is saved.
- Toolbox icons for the ten controls (the standard Windows Forms icons instead of the gear).
- Default properties: `Text` for YANTxt, `Value` for YANNb and `Items` for YANDdl. Default binding properties: `Text`
  for YANTxt and YANDdl and `Value` for YANNb, and YANDdl has lookup binding properties (`DataSource`, `DisplayMember`,
  `ValueMember`, `SelectedValue`) for the Data Sources window.
- `YANTxt.PlaceholderText` and `YANDdl.String` are localizable.

### Changed

- The project is SDK-style (`net481`, C# 12). The package metadata moved from `YANF.nuspec` into `YANF/YANF.csproj`;
  the ClickOnce settings, `App.config`, the unused `Settings`, `AssemblyInfo.cs`, `nuget.exe` and the duplicate `res/`
  folder are gone. Build with Visual Studio 2022 or `msbuild` (see the README).
- The demo is a separate project, `samples/YANF.Demo`, which uses only the public API.
- `YANDdl`: the `[DefaultValue]` of `AutoCompleteMode`, `AutoCompleteSource` and `DropDownStyle` now match the
  constructor. The designer stops writing these properties at their defaults, and a value that differs from them (for
  example `AutoCompleteMode.None`) is now written, where it was silently lost before.
- `YANMath.Min` / `Max` compare with `Comparer<T>.Default`: strings and any `IComparable` type work, the first of equal
  values is returned, and an empty or null array throws `ArgumentException` / `ArgumentNullException` (was
  `IndexOutOfRangeException` / `NullReferenceException`).
- Repository: `.editorconfig`; `.gitattributes` no longer routes images through Git LFS.

Message box

- ENTER presses the default button, and ESC presses Cancel, or OK in an OK-only box, like the Windows message box.
  Yes/No and Abort/Retry/Ignore boxes have no ESC button. The close box (✕) still returns Cancel (see `StrictClose`).
- The default button is marked with a white border instead of an underlined font, and the border moves to the button
  that has the focus (the one ENTER presses).
- Fonts: English boxes and boxes without a language use the fonts of the box's designer file and create none. Vietnamese
  and Japanese boxes create their three fonts once per box, share them between the buttons and dispose them with the box
  (1.0 created six fonts for every box, plus an underlined copy for the default button, and never disposed them). The
  text looks the same apart from the underline.
- Long messages wrap at about half the width of the screen's working area instead of making the box wider than the
  screen.
- Every `Show` overload with an owner shows the box on the owner's UI thread when the owner is a control of another UI
  thread (the calling thread waits for the result), instead of creating the box on the calling thread.
- An invalid `MessageBoxButtons` value throws `InvalidEnumArgumentException` (1.0 showed a box without buttons), and an
  invalid `MsgBoxLang` value gives English buttons (1.0 showed none).

Screens

- `YANLoadScreen`, `YANWaitScreen` and `YANUpdateScreen` derive from the new `YANOverlayScreen`, which derives from
  `MiddleScreen`, so code compiled against 1.0 still sees them as `MiddleScreen`. Their constructors, their public
  fields (`lblPercent`, `lblCapacity`, `pnlProgressBar`), `Frm_Close` and their designer files are unchanged, and their
  fade-in still runs before any other `Shown` handler.
- The Update screen sizes its progress bar from its own width when it is driven by `SetProgress` or `YANLoader`.
  `YANConstant.W_UPDATE_SCR` is needed only with the legacy `YANUpdScrService.PublishValue`, which still uses the width
  it is given.

Controls

- YANBtn, YANTg, YANRdo and YANDp no longer force `TabStop = false`, and YANBtn no longer forces `TabIndex = 0`.
  Existing forms keep their tab order: `TabStop` defaults to true in the designer, so every 1.0 designer file already
  writes `TabStop = false` for these controls. Controls dropped from now on, and controls created in code, can be
  reached with TAB (a YANBtn only when `Focusable` is true), and a YANBtn created in code gets the next `TabIndex`
  instead of 0. YANRdo keeps the `RadioButton` rules. To reach an existing control with TAB, set `TabStop = true` (and
  `Focusable = true` on a YANBtn).
- `YANTxt.Text` and `YANDdl.Text` get and set the content of the control; before, they were the unrelated `Text` of
  `UserControl`, normally empty.
- The default event, which the designer creates on a double-click, is `TextChanged` for YANTxt and
  `SelectedIndexChanged` for YANDdl.
- YANTxt suppresses ENTER only when the box is single-line and `AcceptsReturn` is false.
- YANDdl draws its border and arrow in HotPink while it has the focus, like YANTxt. Set `BorderFocusColor` and
  `IconFocusColor` to `Color.Empty` to turn this off.
- YANRdo draws its text with `TextRenderer` (GDI) instead of `Graphics.DrawString`, at the same position and in the same
  colours, so the glyphs match the standard controls and the drawn text matches its measured size. `&` is still drawn
  as it is.
- Property grid categories: `MaxLength`, `Multiline` and `UseSystemPasswordChar` of YANTxt and the `AutoComplete*`
  properties of YANDdl are in "YAN Behavior"; `Minimum`, `Maximum`, `Value` and `Increment` of YANNb are in "YAN Data".
  Wrong or copied property descriptions are corrected (for example `YANPrg.ChannelHeight`).

Helpers

- `YANEvent.MoveFrm_MouseDown`, `MoveFrm_MouseMove` and `MoveFrm_MouseUp` (now hidden, see Deprecated) react to the left
  button only, track the drag per control, no longer repaint the form on every mouse move, and give the form back the
  opacity it had before the 0.7 dim instead of always setting it to 1.

Demo and documentation

- The sample app uses the new APIs: `RunWithLoaderAsync` and `YANLoader.Show` for the screens, `EnableFade`,
  `EnableDrag`, `GetAllObjs<T>`, `YANMessageBoxOptions`, and a focusable Save button as the `AcceptButton` of Demo1. Its
  "Use 1.0 services (legacy)" switch runs the 1.0 screen services instead.
- The README presents `YANLoader`, `YANMessageBoxOptions`, `EnableFade` / `FadeToAsync`, `EnableDrag` and
  `GetAllObjs<T>` as the recommended APIs, and keeps the 1.0 screen service example, marked as legacy.

### Deprecated

These members are hidden from IntelliSense with `[EditorBrowsable(EditorBrowsableState.Never)]` (and, where noted, from
the property grid). Following the policy above they are not marked `[Obsolete]`: existing code, designer files and
compiled binaries keep compiling and working as before.

- `YANDisplay.AnimateWindow` (the raw user32 import): use `FadeToAsync` or `EnableFade`.
- `YANDisplay.CreateRoundRectRgn` (the raw gdi32 import): use `SetRoundRegion`.
  `Region.FromHrgn(CreateRoundRectRgn(...))` leaks a GDI handle on every call.
- `YANEvent.MoveFrm_MouseDown`, `YANEvent.MoveFrm_MouseMove` and `YANEvent.MoveFrm_MouseUp`: call
  `control.EnableDrag()` once instead of wiring the three handlers.
- The ten `YANMessageBoxScreen` constructors that take a caption, text, buttons, icon, default button or language: use
  `YANMessageBoxScreen(YANMessageBoxOptions)`, or let `YANMessageBox.Show(owner, options)` create the form.
- `YANTxt.String`: use `Text`. It is also hidden from the property grid and no longer written by the designer: existing
  `String = ...` lines still work, and the designer writes `Text` the next time the form is saved. `String` keeps its
  1.0 value (null while the box shows its placeholder).
- `YANTxt.StringChanged`: use `TextChanged`. It is still raised, right after `TextChanged`, with the inner `TextBox` as
  sender. Hidden from the Events list of the property grid; handlers already wired in designer files still run.
- `YANTxt.PasswordChar`: use `UseSystemPasswordChar` (the same value). Hidden from the property grid; the designer
  writes `UseSystemPasswordChar` the next time the form is saved.
- `YANDdl.OnSelectedIndexChanged` (an event, despite its name): use `SelectedIndexChanged`. It is still raised, just
  before `SelectedIndexChanged`, with the inner `ComboBox` as sender. Hidden from the Events list of the property grid;
  handlers already wired in designer files still run.
- `YANDdl.StringChanged`: use `TextChanged`. It is still raised when the text of the combo box changes, with the inner
  `ComboBox` as sender. Hidden from the Events list of the property grid.

Not deprecated: `YANDisplay.FadeIn` / `FadeOut` keep working as before (they block the UI thread while they fade;
`EnableFade` and `FadeToAsync` do not), and the screen services (`YANLoadScrService`, `YANWaitScrService`,
`YANUpdScrService`) remain the choice for work that blocks the UI thread, because they run the screen on a thread of its
own.

### Fixed

Screen services (`YANLoadScrService`, `YANWaitScrService`, `YANUpdScrService`)

- The overlay runs on its own background STA thread and every access to it is marshalled to that thread, so the
  services are thread-safe: `PublishValue` and `OffLoader` can be called from the UI thread or from worker threads.
- `OnLoader` returns once the screen has loaded (or failed, which is rethrown to the caller; it waits 5 seconds at
  most), so `OffLoader` right after `OnLoader` closes it and `PublishValue` never hits a screen that does not exist yet.
- `PublishValue` before `OnLoader` or after `OffLoader` does nothing; when values arrive faster than the screen
  repaints, the latest one wins, and the last value is shown before the screen closes.
- Calling `OnLoader` again closes the previous overlay; calling `OffLoader` more than once is safe.
- The overlay never gets an owner on another thread and no longer keeps the process alive after the application exits.
- `YANUpdScrService` centres the update box on the parent form (it ignored the parent).
- The Load and Wait screens free the GDI region handle of their rounded corners.

Screens

- `MiddleScreen.Frm_Close()` closes the form; 1.0 threw `NotImplementedException`.
- The Load, Wait and Update screens rebuild their rounded corners when they are resized.

Message box

- An OK-only box with `MessageBoxDefaultButton.Button2` or `Button3` focused a hidden button, so ENTER did nothing; the
  default button now falls back to the first button, as two-button boxes already did with `Button3`.
- The Click handler of the close box was wired twice and ran twice.
- Every box leaked six or seven fonts and one or two bitmaps. The fonts a box creates and its icon bitmap are now
  disposed with it.

Controls

- Rounded shapes clamp the radius at paint time: no more exceptions from `AddArc` or `LinearGradientBrush` at small
  sizes or when the border is larger than the radius. Resizing no longer shrinks the configured `BorderRadius` and
  `BorderSize` for good, and negative values are stored as 0.
- Regions are rebuilt when the size or shape changes instead of in `OnPaint` (which caused an endless repaint loop), and
  replaced regions are disposed.
- `YANBtn`: no `NullReferenceException` without a parent, no extra `Parent.BackColorChanged` subscription each time the
  handle is recreated; the hand cursor comes from `DefaultCursor`, so a `Cursor` set by the application is kept.
- `YANGradPnl` paints its gradient as the background, with double buffering and repaint on resize.
- `YANPrg` keeps repainting after `Value` reached `Maximum` and handles `Minimum == Maximum`.
- `YANDp` paints into the paint event's graphics (it drew on `CreateGraphics()`, so printing and `DrawToBitmap` got
  nothing) and recomputes the calendar button area on resize and format changes.
- `YANTg` disposes its pens and brushes; `YANTg` and `YANRdo` show the hand cursor through `DefaultCursor`.
- `YANRdo` highlights the text on hover without changing `ForeColor`, so a `ForeColor` set while hovering survives.
- `YANCirPic` stays square unless it is docked (`Dock` other than `None`), and paints the circle in the largest centred
  square.
- `YANNb`: `Minimum` and `Maximum` go straight to the inner `NumericUpDown`, so the designer order (Maximum, then
  Minimum) works for negative ranges; `String` follows values typed or spun by the user.
- `YANTxt` draws its placeholder instead of writing it into the text: setting `PlaceholderText` keeps the text,
  passwords stay masked, `StringChanged` no longer fires on focus changes, and Enter is suppressed only in single-line
  mode.
- `YANDdl`: the `DropDownStyle` setter tested the current style instead of the new one, so `Simple` was accepted and
  every later value ignored; now `Simple` is ignored and the other values always apply.
- `YANBtn` clicks as the form's `AcceptButton` or `CancelButton` (ENTER, ESC), with an `&` mnemonic and with
  `PerformClick()`; before, all of these did nothing. Like a `Button`, the accept button validates the focused control
  before its Click. Only calls compiled against this version through a `YANBtn` or `IButtonControl` reference use the
  new `PerformClick`: a call through a `Button` reference, or code compiled against 1.0 and not rebuilt, still does
  nothing unless `Focusable` is true.
- `YANDdl`: clearing the text from code or through a binding, or clearing the selection (`SelectedIndex = -1`, also in
  the DropDownList style), shows the prompt again instead of the old text or an empty label. Deleting typed text while
  the control has the focus no longer leaves its last character in the label, and a prompt set through `String` after
  the control was first focused is shown again when the text is cleared.

Helpers

- `YANDisplay.HighLightLblLinkByCtrl` creates a `Font` only when the bold state changes (it leaked one on every call),
  keeps the label's other style bits, never disposes a font it did not create, and does nothing instead of throwing
  when the control is not on a form, its name is too short or the matching control is not a `Label`.
- `YANMath` works for strings (it threw `RuntimeBinderException`).
- `YANEvent.MoveFrm_*`: the form is no longer left dimmed and following the mouse when the MouseUp is lost. The drag
  also ends when the control loses the mouse capture, when the form is deactivated, on a mouse move without the left
  button held, and when the control or the form is disposed.

Demo (now `samples/YANF.Demo`)

- The age is right before the birthday; the Back button gets its normal image back on mouse leave; hover images are
  loaded once and disposed with the forms.

Documentation

- The README no longer wraps `PublishValue` in `Invoke` on the calling form, warns against 1.0.2 and pins its install
  commands to 1.0.1, and links its images absolutely so that they show on the nuget.org package page.

### Known issues

- Since 1.0: `YANBtn.FlatStyle = Standard`, `YANBtn.FlatAppearance.BorderSize = 1` and `YANCirPic.SizeMode = Normal`
  chosen in the designer are not kept at run time. They are the framework defaults, so the designer does not write
  them, and the constructor's values (`Flat`, 0, `StretchImage`) apply. Set them in code after `InitializeComponent()`.

## [1.0.2] - 2023-05-24

### Security

- **Broken package, do not use.** It has no `YANF.dll` under `lib/`, so it adds no reference, and it ships build output
  and tooling. Use 1.0.1 or a later version.

## [1.0.1] - 2022-10-30

- No release notes were recorded.

## [1.0.0] - 2022-10-20

- First release.

[Unreleased]: https://github.com/Tynab/YANF/commits/main
[1.0.2]: https://www.nuget.org/packages/Tynab.YANF/1.0.2
[1.0.1]: https://www.nuget.org/packages/Tynab.YANF/1.0.1
[1.0.0]: https://www.nuget.org/packages/Tynab.YANF/1.0.0
