# YAMI AN NEPHILIM FRAMEWORK
YANF is based on .NET Framework 4.8.1, YANF use for Windows Forms App project with C# or Visual Basic programming languages.

### INSTALL
https://www.nuget.org/packages/Tynab.YANF
```
PM> NuGet\Install-Package Tynab.YANF
```

## IMAGE DEMO
<p align="center">
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/1.jpg"></img>
</p>

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
<p align="center">
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/3.jpg"></img>
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/4.jpg"></img>
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/5.jpg"></img>
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/6.jpg"></img>
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/7.jpg"></img>
</p>

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
    _ = Invoke((MethodInvoker)delegate
    {
        _dlvScrService.PublishValue(_percent, null, 0);
    });
    ...
}
```
<p align="center">
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/8.jpg"></img>
</p>

```c#
/* Call test screen */
using YANF.Script;

// Method
private void Func()
{
    ...
    new MainFrm().Show();
    ...
}
```
<p align="center">
<img src="https://raw.githubusercontent.com/Tynab/YANF/main/pic/2.jpg"></img>
</p>

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

[See wiki for more details](https://github.com/Tynab/YANF/wiki)