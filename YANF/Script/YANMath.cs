using System;
using System.Collections.Generic;

namespace YANF.Script
{
    public static class YANMath
    {
        /// <summary>
        /// Tìm giá trị nhỏ nhất.
        /// </summary>
        /// <param name="list">Chuỗi dữ liệu so sánh.</param>
        /// <returns>Giá trị nhỏ nhất.</returns>
        /// <remarks>Values are compared with <see cref="Comparer{T}.Default"/> (<see cref="IComparable{T}"/> or <see cref="IComparable"/>); the first of several equal minimums is returned.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is empty, or <typeparamref name="T"/> is not comparable.</exception>
        public static T Min<T>(params T[] list) => Pick(list, -1);

        /// <summary>
        /// Tìm giá trị lớn nhất.
        /// </summary>
        /// <param name="list">Chuỗi dữ liệu so sánh.</param>
        /// <returns>Giá trị lớn nhất.</returns>
        /// <remarks>Values are compared with <see cref="Comparer{T}.Default"/> (<see cref="IComparable{T}"/> or <see cref="IComparable"/>); the first of several equal maximums is returned.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="list"/> is empty, or <typeparamref name="T"/> is not comparable.</exception>
        public static T Max<T>(params T[] list) => Pick(list, 1);

        // Pick the first item whose comparison sign matches (-1 = min, 1 = max)
        private static T Pick<T>(T[] list, int sign)
        {
            if (list is null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (list.Length == 0)
            {
                throw new ArgumentException("At least one value is required.", nameof(list));
            }
            var cmp = Comparer<T>.Default;
            var res = list[0];
            for (var i = 1; i < list.Length; i++)
            {
                if (Math.Sign(cmp.Compare(list[i], res)) == sign)
                {
                    res = list[i];
                }
            }
            return res;
        }
    }
}
