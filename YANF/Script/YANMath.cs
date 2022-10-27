namespace YANF.Script
{
    public static class YANMath
    {
        /// <summary>
        /// Tìm giá trị nhỏ nhất.
        /// </summary>
        /// <param name="list">Chuỗi dữ liệu so sánh.</param>
        /// <returns>Giá trị nhỏ nhất.</returns>
        public static T Min<T>(params T[] list)
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
        public static T Max<T>(params T[] list)
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
