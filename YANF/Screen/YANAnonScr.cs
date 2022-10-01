using System.Windows.Forms;

namespace YANF.Screen;

public abstract class YANAnonScr : YANHardScr
{
    #region Overridden
    // Ẩn sub window
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80;
            return cp;
        }
    }
    #endregion

    #region Methods
    /// <summary>
    /// Form close event.
    /// </summary>
    public abstract void Frm_Close();
    #endregion
}
