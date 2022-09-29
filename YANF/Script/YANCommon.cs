using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YANF.Script
{
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
    }
}
