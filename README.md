# YAMI AN NEPHILIM FRAMEWORK
YANF is based on .NET Framework, YANF use for Windows Forms App project with C# or Visual Basic programming languages.
<p align="center">
<img src="https://raw.githubusercontent.com/Tynab/Loginside-FYAN-Bot/main/pic/0.gif"></img>
</p>

### INSTALL
https://www.nuget.org/packages/Tynab.YANF
```
PM> NuGet\Install-Package Tynab.YANF
```

## IMAGE DEMO
<p align="center">
<img src="https://raw.githubusercontent.com/Tynab/Loginside-FYAN-Bot/main/pic/0.gif"></img>
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
}
```
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
}

// Method
private void Func()
{
    ...
    _ = Invoke((MethodInvoker)delegate
    {
        _dlvScrService.PublishValue(_percent, null, 0);
    });
}
```
```c#
/* Call test screen */
using YANF.Script;

// Method
private void Func()
{
    ...
    new MainFrm().Show();
}
```

### FORM
- MessageBox new style (support for 3 languages)
    - English
    - 日本語
    - Tiếng Việt
- Wait screen
- Load screen
- Update screen

### CONTROL
- Button new style
- CirclePictureBox
- DropdownList
- GradientPanel
- NumBox
- ProgressBar new style
- RadioButton new style
- ToggleButton
- TextBox new style

### UTILITIES
- Display
    - CreateRoundRectRgn
    - AnimateWindow
    - FadeIn
    - FadeOut
    - HighLightLblLinkByCtrl
- Math
    - Min
    - Max
- String
    - ParseDouble
    - ParseInt
- Timer
    - StartAdv
    - StopAdv
- Event
    - MoveFrm_MouseDown
    - MoveFrm_MouseMove
    - MoveFrm_MouseUp