using System;
using System.Globalization;
using System.Threading;
using Xunit;
using YANF.Script;

namespace YANF.Tests.Script
{
    public class YANStringTests
    {
        private static readonly CultureInfo Vi = new("vi-VN");
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [Fact]
        public void CultureData_IsWhatTheTestsAssume()
        {
            Assert.Equal(",", Vi.NumberFormat.NumberDecimalSeparator);
            Assert.Equal(".", Vi.NumberFormat.NumberGroupSeparator);
        }

        // The 1.0.2 overloads keep parsing with the current culture and returning 0 on failure
        [Fact]
        public void LegacyOverloads_Unchanged()
        {
            WithCulture(Vi, () =>
            {
                Assert.Equal(1.5, "1,5".ParseDouble());
                Assert.Equal(1234.5, "1.234,5".ParseDouble());
                Assert.Equal(0, "abc".ParseDouble());
                Assert.Equal(0, ((string)null).ParseDouble());
                Assert.Equal(42, "42".ParseInt());
                Assert.Equal(0, "x".ParseInt());
                Assert.Equal(0, "1.000".ParseInt());
            });
            WithCulture(Inv, () =>
            {
                Assert.Equal(1.5, "1.5".ParseDouble());
                Assert.Equal(-7, "-7".ParseInt());
            });
        }

        [Theory]
        [InlineData("vi-VN")]
        [InlineData("")]
        public void TryParseDouble_UsesTheGivenCulture(string current) => WithCulture(new CultureInfo(current), () =>
        {
            Assert.True("1,5".TryParseDouble(Vi, out var v) && v == 1.5, "vi 1,5");
            Assert.True("1.234,5".TryParseDouble(Vi, out v) && v == 1234.5, "vi thousands");
            Assert.True("1.5".TryParseDouble(Inv, out v) && v == 1.5, "invariant 1.5");
            Assert.True("1,234.5".TryParseDouble(Inv, out v) && v == 1234.5, "invariant thousands");
            Assert.True("-2.5e3".TryParseDouble(Inv, out v) && v == -2500, "invariant exponent");
            Assert.True("0".TryParseDouble(Inv, out v) && v == 0, "a real 0 succeeds");
            Assert.False("abc".TryParseDouble(Inv, out v), "failure");
            Assert.Equal(0, v);
            Assert.False("".TryParseDouble(Vi, out _), "empty");
            Assert.False(((string)null).TryParseDouble(Vi, out _), "null");
        });

        [Fact]
        public void TryParseDouble_NullProviderUsesCurrentCulture()
        {
            WithCulture(Vi, () => Assert.True("1,5".TryParseDouble(null, out var v) && v == 1.5));
            WithCulture(Inv, () => Assert.True("1.5".TryParseDouble(null, out var v) && v == 1.5));
        }

        [Theory]
        [InlineData("vi-VN")]
        [InlineData("")]
        public void TryParseInt_UsesTheGivenCulture(string current) => WithCulture(new CultureInfo(current), () =>
        {
            Assert.True("42".TryParseInt(Inv, out var n) && n == 42, "invariant 42");
            Assert.True(" -5 ".TryParseInt(Vi, out n) && n == -5, "vi -5 with white space");
            Assert.True("0".TryParseInt(Vi, out n) && n == 0, "a real 0 succeeds");
            Assert.False("1.000".TryParseInt(Vi, out n), "the Integer style has no thousands");
            Assert.Equal(0, n);
            Assert.False("1.5".TryParseInt(Inv, out _), "no decimals");
            Assert.False("99999999999".TryParseInt(Inv, out _), "overflow fails");
            Assert.False(((string)null).TryParseInt(Inv, out _), "null fails");
        });

        [Fact]
        public void ParseWithFallback() => WithCulture(Vi, () =>
        {
            // an explicit provider wins over the vi-VN current culture
            Assert.Equal(2.5, "2.5".ParseDouble(Inv, -1));
            Assert.Equal(2.5, "2,5".ParseDouble(Vi, -1));
            Assert.Equal(-1, "x".ParseDouble(Inv, -1));
            Assert.True(double.IsNaN(((string)null).ParseDouble(Vi, double.NaN)));
            Assert.Equal(0, "0".ParseDouble(Inv, -1));
            Assert.Equal(42, "42".ParseInt(Inv, -1));
            Assert.Equal(-1, "x".ParseInt(Vi, -1));
            Assert.Equal(-1, "99999999999".ParseInt(Inv, -1));
            Assert.Equal(0, "0".ParseInt(Vi, -1));
        });

        // Runs with the given current culture, then restores it
        private static void WithCulture(CultureInfo culture, Action body)
        {
            var old = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = culture;
            try
            {
                body();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = old;
            }
        }
    }
}
