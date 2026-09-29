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
### MESSAGEBOX
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
```c#
/* Show Load screen */
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

### DEMO APP
The demo forms (`MainFrm`, `Demo1`, `Demo2`) are no longer part of the package (earlier versions shipped them inside `YANF.dll`).
They live in the sample project [samples/YANF.Demo](https://github.com/Tynab/YANF/tree/main/samples/YANF.Demo): open `YANF.sln` and run `YANF.Demo`.

![Demo app](https://raw.githubusercontent.com/Tynab/YANF/main/pic/2.jpg)

### FORM
- MessageBox new style (support for 3 languages)
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