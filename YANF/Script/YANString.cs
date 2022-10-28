namespace YANF.Script
{
    public static class YANString
    {
        /// <summary>
        /// Chuyển chuỗi sang số double.
        /// </summary>
        /// <returns>Số kiểu double.</returns>
        public static double ParseDouble(this string str)
        {
            double.TryParse(str, out var num);
            return num;
        }

        /// <summary>
        /// Chuyển chuỗi sang số int.
        /// </summary>
        /// <returns>Số kiểu int.</returns>
        public static int ParseInt(this string str)
        {
            int.TryParse(str, out var num);
            return num;
        }
    }
}
