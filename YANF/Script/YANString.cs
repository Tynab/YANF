using System;
using System.Globalization;
using static System.Globalization.NumberStyles;

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

        /// <summary>
        /// Tries to convert a string to a <see cref="double"/> using the given culture-specific format
        /// (<see cref="NumberStyles.Float"/> | <see cref="NumberStyles.AllowThousands"/>, the same styles as <see cref="ParseDouble(string)"/>).
        /// </summary>
        /// <param name="str">The string to convert. Null or empty fails.</param>
        /// <param name="provider">The format to parse with, e.g. <see cref="CultureInfo.InvariantCulture"/> or <c>new CultureInfo("vi-VN")</c>; null means <see cref="CultureInfo.CurrentCulture"/>.</param>
        /// <param name="result">The parsed value, or 0 when the conversion fails.</param>
        /// <returns>True if <paramref name="str"/> was converted; false otherwise, so a real 0 can be told apart from a failure.</returns>
        public static bool TryParseDouble(this string str, IFormatProvider provider, out double result) => double.TryParse(str, Float | AllowThousands, provider, out result);

        /// <summary>
        /// Tries to convert a string to an <see cref="int"/> using the given culture-specific format
        /// (<see cref="NumberStyles.Integer"/>, the same style as <see cref="ParseInt(string)"/>).
        /// </summary>
        /// <param name="str">The string to convert. Null or empty fails.</param>
        /// <param name="provider">The format to parse with, e.g. <see cref="CultureInfo.InvariantCulture"/>; null means <see cref="CultureInfo.CurrentCulture"/>.</param>
        /// <param name="result">The parsed value, or 0 when the conversion fails.</param>
        /// <returns>True if <paramref name="str"/> was converted; false otherwise, so a real 0 can be told apart from a failure.</returns>
        public static bool TryParseInt(this string str, IFormatProvider provider, out int result) => int.TryParse(str, Integer, provider, out result);

        /// <summary>
        /// Converts a string to a <see cref="double"/> using the given culture-specific format, or returns <paramref name="fallback"/> when it cannot be converted.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="provider">The format to parse with, e.g. <see cref="CultureInfo.InvariantCulture"/>; null means <see cref="CultureInfo.CurrentCulture"/>.</param>
        /// <param name="fallback">The value returned when <paramref name="str"/> is null, empty or not a number in that format.</param>
        /// <returns>The parsed value, or <paramref name="fallback"/>.</returns>
        public static double ParseDouble(this string str, IFormatProvider provider, double fallback) => str.TryParseDouble(provider, out var num) ? num : fallback;

        /// <summary>
        /// Converts a string to an <see cref="int"/> using the given culture-specific format, or returns <paramref name="fallback"/> when it cannot be converted.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="provider">The format to parse with, e.g. <see cref="CultureInfo.InvariantCulture"/>; null means <see cref="CultureInfo.CurrentCulture"/>.</param>
        /// <param name="fallback">The value returned when <paramref name="str"/> is null, empty, out of range or not a number in that format.</param>
        /// <returns>The parsed value, or <paramref name="fallback"/>.</returns>
        public static int ParseInt(this string str, IFormatProvider provider, int fallback) => str.TryParseInt(provider, out var num) ? num : fallback;
    }
}
