using System;
using static System.Windows.Forms.DialogResult;
using static YANF.Script.YANAnimation;
using static YANF.Script.YANConstant.AnimateWindowFlags;

namespace YANF.Screen;

public partial class YANUpdateScr : MiddleScr
{
    #region Constructors
    public YANUpdateScr()
    {
        InitializeComponent();
        // property
        labelCapacity.Text = null;
        labelPercent.Text = null;
        panelProgressBar.Width = 1;
        // event
        Load += YANUpdateScreen_Load;
    }
    #endregion

    #region Overridden
    // Implementation Frm_Close
    public override void Frm_Close()
    {
        DialogResult = OK;
        AnimateWindow(Handle, 500, AW_BLEND | AW_HIDE);
        Dispose();
    }
    #endregion

    #region Events
    // Load
    private void YANUpdateScreen_Load(object sender, EventArgs e) => AnimateWindow(Handle, 500, AW_BLEND);
    #endregion
}
