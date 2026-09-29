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

Helpers

- `YANDisplay.HighLightLblLinkByCtrl` creates a `Font` only when the bold state changes (it leaked one on every call),
  keeps the label's other style bits, never disposes a font it did not create, and does nothing instead of throwing
  when the control is not on a form, its name is too short or the matching control is not a `Label`.
- `YANMath` works for strings (it threw `RuntimeBinderException`).

Demo (now `samples/YANF.Demo`)

- The age is right before the birthday; the Back button gets its normal image back on mouse leave; hover images are
  loaded once and disposed with the forms.

Documentation

- The README no longer wraps `PublishValue` in `Invoke` on the calling form, warns against 1.0.2 and pins its install
  commands to 1.0.1, and links its images absolutely so that they show on the nuget.org package page.

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
