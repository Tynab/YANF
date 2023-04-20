using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Drawing.Drawing2D.SmoothingMode;
using static System.Math;
using static System.Windows.Forms.ComboBoxStyle;

namespace YANF.Control;

public partial class YANDdl
{
    #region Ctrl
    // Raises the key up event
    private void Ctrl_KeyUp(object sender, KeyEventArgs e) => OnKeyUp(e);

    // Check border size and radius when resize the control
    private void Ctrl_Resize(object sender, EventArgs e)
    {
        var minSize = Width > Height ? Height : Width;
        _borderSize = Min(_borderSize, minSize / 2);
    }
    #endregion

    #region Cmb
    // Raises the selected index changed event
    private void Cmb_SelectedIndexChanged(object sender, EventArgs e)
    {
        _lblText.Text = _cmbList.Text;
        OnSelectedIndexChanged?.Invoke(sender, e);
    }

    // Raises the text changed event
    private void Cmb_StringChanged(object sender, EventArgs e) => StringChanged?.Invoke(sender, e);

    // Raises the text changed event
    private void Cmb_TextChanged(object sender, EventArgs e)
    {
        if (_cmbList.DropDownStyle != DropDownList && !string.IsNullOrWhiteSpace(_cmbList.Text))
        {
            _lblText.Text = _cmbList.Text;
        }
    }
    #endregion

    #region Btn
    // Icon paint event
    private void Ic_Paint(object sender, PaintEventArgs e)
    {
        var wIc = 14;
        var hIc = 6;
        var rectIc = new Rectangle((_btnIc.Width - wIc) / 2, (_btnIc.Height - hIc) / 2, wIc, hIc);
        var graphics = e.Graphics;
        // draw arrow down icon
        using var path = new GraphicsPath();
        using var pen = new Pen(_iconColor, 2);
        graphics.SmoothingMode = AntiAlias;
        path.AddLine(rectIc.X, rectIc.Y, rectIc.X + wIc / 2, rectIc.Bottom);
        path.AddLine(rectIc.X + wIc / 2, rectIc.Bottom, rectIc.Right, rectIc.Y);
        graphics.DrawPath(pen, path);
    }

    // Icon click event
    private void Ic_Click(object sender, EventArgs e)
    {
        _cmbList.Select();
        _cmbList.DroppedDown = true;
    }
    #endregion

    #region Lbl
    // Raises the mouse enter event
    private void Surface_MouseEnter(object sender, EventArgs e) => OnMouseEnter(e);

    // Raises the mouse leave event
    private void Surface_MouseLeave(object sender, EventArgs e) => OnMouseLeave(e);

    // Raises the click event
    private void Surface_Click(object sender, EventArgs e)
    {
        OnClick(e);
        _cmbList.Select();
        if (_cmbList.DropDownStyle == DropDownList)
        {
            _cmbList.DroppedDown = true;
        }
    }
    #endregion

    #region Ddl
    // Raises the enter event
    private void Ddl_Enter(object sender, EventArgs e)
    {
        if (_cmbList.DropDownStyle != DropDownList)
        {
            if (string.IsNullOrWhiteSpace(_string))
            {
                _string = _lblText.Text;
            }
            _lblText.Text = null;
        }
    }

    // Raises the leave event
    private void Ddl_Leave(object sender, EventArgs e)
    {
        if (_cmbList.DropDownStyle != DropDownList && string.IsNullOrWhiteSpace(_lblText.Text))
        {
            _lblText.Text = _string;
        }
    }
    #endregion
}
