using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YANF.Script;

public static class YANCommon
{
    /// <summary>
    /// Tìm giá trị nhỏ nhất.
    /// </summary>
    /// <param name="list">Chuỗi dữ liệu so sánh.</param>
    /// <returns>Giá trị nhỏ nhất.</returns>
    public static T Miner<T>(params T[] list)
    {
        dynamic res = list[0];
        foreach (var item in list)
        {
            if (item < res)
            {
                res = item;
            }
        }
        return res;
    }

    /// <summary>
    /// Tìm giá trị lớn nhất.
    /// </summary>
    /// <param name="list">Chuỗi dữ liệu so sánh.</param>
    /// <returns>Giá trị lớn nhất.</returns>
    public static T Maxer<T>(params T[] list)
    {
        dynamic res = list[0];
        foreach (var item in list)
        {
            if (item > res)
            {
                res = item;
            }
        }
        return res;
    }

    /// <summary>
    /// Invoke text tới label khác thread.
    /// </summary>
    /// <param name="text">Text cần invoke.</param>
    public static void InvokeText(this Label lbl, string text) => lbl.Invoke((MethodInvoker)(() => lbl.Text = text));

    /// <summary>
    /// Invoke độ rộng tới panel khác thread.
    /// </summary>
    /// <param name="w">Độ rộng cần invoke.</param>
    public static void InvokeW(this Panel pnl, int w) => pnl.Invoke((MethodInvoker)(() => pnl.Width = w));
}
