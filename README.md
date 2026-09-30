# YAMI AN NEPHILIM FRAMEWORK
YANF is based on .NET Framework 4.8.1, YANF use for Windows Forms App project with C# or Visual Basic programming languages.

### INSTALL
https://www.nuget.org/packages/Tynab.YANF
```
PM> NuGet\Install-Package Tynab.YANF -Version 1.0.1
```
```
> dotnet add package Tynab.YANF --version 1.0.1
```
**Do not use version 1.0.2**: that package has no `YANF.dll` under `lib/`, so it adds no reference. It is currently the latest version on nuget.org, so the commands above pin 1.0.1.
Release notes and the versioning policy are in [CHANGELOG.md](https://github.com/Tynab/YANF/blob/main/CHANGELOG.md).

## IMAGE DEMO
![Image demo](https://raw.githubusercontent.com/Tynab/YANF/main/pic/1.jpg)

## CODE DEMO
Examples marked **Recommended** use APIs that are newer than 1.0.1 (see the Unreleased section of [CHANGELOG.md](https://github.com/Tynab/YANF/blob/main/CHANGELOG.md)). The 1.0 APIs in the other examples keep working.

### MESSAGEBOX
```c#
/* Show English MessageBox with options (Recommended) */
using YANF.Script;
using static System.Windows.Forms.MessageBoxButtons;
using static System.Windows.Forms.MessageBoxDefaultButton;
using static System.Windows.Forms.MessageBoxIcon;
using static YANF.Script.YANConstant.MsgBoxLang;

// Button click
private void BtnExit_Click(object sender, EventArgs e)
{
    var options = new YANMessageBoxOptions
    {
        Caption = "WARNING",
        Text = "If you close this window, all data will be lost!",
        Buttons = OKCancel,
        Icon = Warning,
        DefaultButton = Button2, // ENTER presses Cancel; ESC presses Cancel too
        Language = ENG           // or VIE, JAP; null = the English default
    };
    // Owned by this form; called from a worker thread, the box is shown on this form's UI thread
    if (YANMessageBox.Show(this, options) == DialogResult.OK)
    {
        Close();
    }
}
```
`YANMessageBoxOptions` also has `TopMost` (default `true`) and `StrictClose` (default `false`; when `true` the close box ✕ gives what ESC gives, and is hidden for YesNo and AbortRetryIgnore).

The 1.0 overloads still work:
```c#
/* Show Japanese MessageBox */
using YANF.Script;
using static System.Windows.Forms.MessageBoxButtons;
using static System.Windows.Forms.MessageBoxIcon;
using static YANF.Script.YANConstant.MsgBoxLang;

// Method
private void Func()
{
    ...
    YANMessageBox.Show("情報", "完了！", OK, Information, JAP);
	...
}
```
![MessageBox](https://raw.githubusercontent.com/Tynab/YANF/main/pic/3.jpg) ![MessageBox](https://raw.githubusercontent.com/Tynab/YANF/main/pic/4.jpg) ![MessageBox](https://raw.githubusercontent.com/Tynab/YANF/main/pic/5.jpg) ![MessageBox](https://raw.githubusercontent.com/Tynab/YANF/main/pic/6.jpg) ![MessageBox](https://raw.githubusercontent.com/Tynab/YANF/main/pic/7.jpg)

### SCREEN
`YANLoader` shows a Load, Wait or Update screen over a form, on the form's own UI thread, while async work runs. The screen appears only when the work is still running after `ShowDelay` (250 ms by default), and follows the form when it moves or is resized. The form takes no input until the work has ended, and always gets it back, also after an exception or a cancellation.
```c#
/* Show Load screen with progress and cancellation (Recommended) */
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YANF.Script;

// Button click (async: the UI thread stays free while the work runs)
private async void BtnLoad_Click(object sender, EventArgs e)
{
    // The form takes no input while the screen is up, so cancel from code: here after 30 seconds
    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
    {
        try
        {
            var options = new YANLoaderOptions
            {
                Kind = YANLoaderKind.Load, // or Wait, Update
                ShowDelay = 250            // short work shows no screen at all
            };
            // The work runs on the thread pool; the screen closes when it ends, also when it fails or is cancelled
            var rows = await this.RunWithLoaderAsync((progress, ct) => Task.Run(() => LoadRows(progress, ct), ct), options, cts.Token);
            ...
        }
        catch (OperationCanceledException)
        {
            ...
        }
    }
}

// Work (worker thread)
private List<string> LoadRows(IProgress<int> progress, CancellationToken ct)
{
    var rows = new List<string>();
    for (var percent = 1; percent <= 100; percent++)
    {
        ct.ThrowIfCancellationRequested();
        ...
        // Thread-safe: the screen shows the latest value
        progress.Report(percent);
    }
    return rows;
}
```
Without options or a token: `await this.RunWithLoaderAsync((progress, ct) => Task.Run(() => Work(progress, ct), ct));`.

The `progress` passed to the work is the `YANLoaderScope` of the screen: cast it to call `SetProgress(percent, detail)`, which also shows a detail text on the Update screen. Around awaited work on the UI thread, use `YANLoader.Show` in a `using` block:
```c#
/* Show Update screen around awaited work (Recommended) */
private async void BtnUpdate_Click(object sender, EventArgs e)
{
    using (var scope = YANLoader.Show(this, new YANLoaderOptions { Kind = YANLoaderKind.Update }))
    {
        for (var percent = 1; percent <= 100; percent++)
        {
            await DownloadPartAsync(percent);
            scope.SetProgress(percent, $"{percent * 137 / 100} MB / 137 MB");
        }
    } // the form gets its input back here, and the screen fades out
}
```
Legacy: the 1.0 screen services still work. Use them for work that blocks the UI thread, because they run the screen on a thread of its own.
```c#
/* Show Load screen (1.0 service) */
using YANF.Script;
using YANF.Script.Service;

// Fields
private IYANDlvScrService _dlvScrService;
private int _percent;

// Button click
private void Btn_Click(object sender, EventArgs e)
{
    ...
    _dlvScrService = new YANLoadScrService();
    _dlvScrService.OnLoader(this);
    this.FadeOut();
    ...
}

// Method
private void Func()
{
    ...
    // Thread-safe: call it from the UI thread or from a worker thread
    _dlvScrService.PublishValue(_percent, null, 0);
    ...
}

// Work done (UI thread)
private void Done()
{
    ...
    // Thread-safe too, and safe to call more than once
    _dlvScrService.OffLoader();
    this.FadeIn();
    ...
}
```
![Load screen](https://raw.githubusercontent.com/Tynab/YANF/main/pic/8.jpg)

### FADE & DRAG
```c#
/* Fade and drag a borderless form (Recommended) */
using YANF.Script;

// Constructor
public LoginFrm()
{
    InitializeComponent();
    // Fade in each time the form is shown and out when it closes (ms), without blocking the UI thread.
    this.EnableFade(250, 250);
    // Drag the form by its header panel and title label (Aero Snap and multiple monitors work)
    pnlHeader.EnableDrag();
    lblTitle.EnableDrag();
}

// Button click
private async void BtnCheck_Click(object sender, EventArgs e)
{
    // Dim the form while it waits, then restore it
    await this.FadeToAsync(0.6, 150);
    ...
    await this.FadeToAsync(1, 150);
}
```
`EnableFade` replaces the 1.0 `FadeIn()` / `FadeOut()` calls in `Shown` and `FormClosing` (do not combine them): the fade no longer blocks the UI thread, a modal form keeps its `DialogResult`, and a cancelled close no longer leaves an invisible form. `EnableDrag` replaces the three handlers `YANEvent.MoveFrm_MouseDown`, `MoveFrm_MouseMove` and `MoveFrm_MouseUp`. A press that drags the form gives the control no `Click`, so use panels, labels or pictures as handles, not buttons.

### FIND CONTROLS
```c#
/* Get controls by type (Recommended) */
using System.Windows.Forms;
using YANF.Control;
using YANF.Script;

// Constructor
public LoginFrm()
{
    InitializeComponent();
    // Every YANTxt of the form, also inside panels
    foreach (var txt in this.GetAllObjs<YANTxt>())
    {
        txt.Enter += Txt_Enter;
    }
    // Derived types too: Panel also finds YANGradPnl
    foreach (var pnl in this.GetAllObjs<Panel>())
    {
        pnl.EnableDrag();
    }
    // Skip the children of YANDdl (its inner label)
    foreach (var lbl in this.GetAllObjs<Label>(c => !(c is YANDdl)))
    {
        lbl.EnableDrag();
    }
}
```
The 1.0 `GetAllObjs(typeof(YANTxt))` still works and matches the exact type only.

### DEMO APP
The demo forms (`MainFrm`, `Demo1`, `Demo2`) are no longer part of the package (earlier versions shipped them inside `YANF.dll`).
They live in the sample project [samples/YANF.Demo](https://github.com/Tynab/YANF/tree/main/samples/YANF.Demo): open `YANF.sln` and run `YANF.Demo`.

![Demo app](https://raw.githubusercontent.com/Tynab/YANF/main/pic/2.jpg)

### FORM
- MessageBox new style (support for 3 languages)
- Loader: Load, Wait or Update screen over a form while async work runs
- Wait screen
- Load screen
- Update screen

### CONTROL
- Button new style
- ProgressBar new style
- RadioButton new style
- TextBox new style
- CirclePictureBox
- DropdownList
- GradientPanel
- NumBox
- ToggleButton

### EXTENSION
- Numeric
- Text
- List
- Random
- Process
- Task
- Display
- Timer
- Event

### OTHER
- Password

## BUILD FROM SOURCE
Requirements: Windows with the .NET Framework 4.8.1 runtime, and Visual Studio 2022 (17.8 or later, workload ".NET desktop development") or Build Tools for Visual Studio with the .NET SDK.

Build in Visual Studio (open `YANF.sln`) or from a Developer Command Prompt. Use Visual Studio's MSBuild: `dotnet build` cannot embed the bitmap resources of the forms for .NET Framework.
```
msbuild YANF.sln -restore -p:Configuration=Release
```
Run the tests (xUnit, `tests/YANF.Tests`) from Test Explorer or after the build. They create and show windows, so they need an interactive desktop.
```
dotnet test tests/YANF.Tests/YANF.Tests.csproj -c Release --no-build
```
Run the demo app by setting `samples/YANF.Demo` as the startup project. Create the package (it is validated against 1.0.1 for breaking changes):
```
msbuild YANF/YANF.csproj -t:Pack -p:Configuration=Release
```
Every push to main and every pull request is built, tested and packed by [GitHub Actions](https://github.com/Tynab/YANF/blob/main/.github/workflows/ci.yml); a `v*` tag publishes the package to nuget.org.

[See wiki for more details](https://github.com/Tynab/YANF/wiki)