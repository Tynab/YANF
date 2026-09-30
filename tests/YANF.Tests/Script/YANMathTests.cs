using System;
using Xunit;
using YANF.Script;

namespace YANF.Tests.Script
{
    // 1.0.2 compared with dynamic: strings threw RuntimeBinderException and every call paid for the binder
    public class YANMathTests
    {
        [Fact]
        public void Ints()
        {
            Assert.Equal(9, YANMath.Max(3, 9, -2, 9, 0));
            Assert.Equal(-2, YANMath.Min(3, 9, -2, 9, 0));
            Assert.Equal(7, YANMath.Max(7));
            Assert.Equal(7, YANMath.Min(7));
            Assert.Equal(int.MaxValue, YANMath.Max(int.MinValue, int.MaxValue));
            Assert.Equal(int.MinValue, YANMath.Min(int.MaxValue, int.MinValue));
            Assert.Equal(8, YANMath.Max(new[] { 4, 1, 8 }));
            // the message box width call shape
            int w = 120, min = 300;
            Assert.Equal(300, YANMath.Max(w, min, 250 + 30 + 5 + 5));
        }

        [Fact]
        public void FloatingPointAndDecimal()
        {
            Assert.Equal(2.25, YANMath.Max(1.5, -0.5, 2.25));
            Assert.Equal(-0.5, YANMath.Min(1.5, -0.5, 2.25));
            Assert.Equal(-1e-300, YANMath.Max(-1e-300, -1e300));
            Assert.Equal(0.1f, YANMath.Min(0.1f, 0.2f));
            Assert.Equal(2.5m, YANMath.Max(1m, 2.5m, 2.4m));
        }

        // As in 1.x (the < and > operators are false for null and NaN): such a value never replaces the result
        [Fact]
        public void NullAndNaN_BehaveAs1x()
        {
            Assert.Equal(1d, YANMath.Min(1.0, double.NaN));
            Assert.True(double.IsNaN(YANMath.Max(double.NaN, 1.0)));
            Assert.Equal(1d, YANMath.Max(1.0, double.NaN, 0.5));
            Assert.Equal(1f, YANMath.Min(1f, float.NaN));
            Assert.Equal(1, YANMath.Min<int?>(1, null, 3));
            Assert.Null(YANMath.Max<int?>(null, 1));
            Assert.Equal(3, YANMath.Max<int?>(1, null, 3));
        }

        [Fact]
        public void Strings()
        {
            Assert.Equal("c", YANMath.Max("b", "a", "c"));
            Assert.Equal("a", YANMath.Min("b", "a", "c"));
            Assert.Null(YANMath.Min<string>((string)null));
        }

        [Fact]
        public void Ties_ReturnTheFirst_AndComparableIsRequired()
        {
            Ver a = new(2, 1), b = new(5, 2), c = new(5, 3), d = new(1, 4), e = new(1, 5);
            Assert.Equal(2, YANMath.Max(a, b, c, d, e).Id);
            Assert.Equal(4, YANMath.Min(a, b, c, d, e).Id);
            var single = new NotComparable();
            Assert.Same(single, YANMath.Max(single));
            Assert.ThrowsAny<ArgumentException>(() => YANMath.Max(new NotComparable(), new NotComparable()));
        }

        [Fact]
        public void EmptyOrNull_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => YANMath.Min<int>());
            Assert.ThrowsAny<ArgumentException>(() => YANMath.Max<int>());
            Assert.ThrowsAny<ArgumentException>(() => YANMath.Max(new double[0]));
            Assert.ThrowsAny<ArgumentException>(() => YANMath.Min<string>(null));
            Assert.ThrowsAny<ArgumentException>(() => YANMath.Max<int>(null));
        }

        private sealed class Ver : IComparable<Ver>
        {
            public Ver(int major, int id)
            {
                Major = major;
                Id = id;
            }

            public int Major { get; }

            public int Id { get; }

            public int CompareTo(Ver other) => Major.CompareTo(other.Major);
        }

        private sealed class NotComparable
        {
        }
    }
}
