using static FakeRdb.TypeAffinity;

namespace FakeRdb.Tests;

public sealed class NumericTests
{
    [Theory]
    [InlineData("100e-3", false)]
    [InlineData("100e-1", true)]
    [InlineData("1.1e3", true)]
    [InlineData("1.1", false)]
    [InlineData("1", true)]
    [InlineData("1e3", true)]
    // Negative
    [InlineData("-1", true)]
    [InlineData("-1.1", false)]
    [InlineData("-1.1e3", true)]
    // Zero
    [InlineData("0", true)]
    [InlineData("0.0", true)]
    [InlineData("0e3", true)]
    [InlineData("0.0e3", true)]
    [InlineData("1E3", true)]
    // Small fractions
    [InlineData("1.000000000000001", false)]
    [InlineData("1.0000000000000001", false)]
    // Very large number, close to the upper limit of double.
    [InlineData("1e308", true)]
    // Very close to zero, lower limit of positive values for double.
    [InlineData("1e-324", false)]

    [InlineData("1e", false)]  // Incomplete scientific notation.
    [InlineData("e1", false)]  // Invalid format.
    [InlineData("1..1", false)]  // Multiple decimal points.
    [InlineData(".1e3", true)]  // Starts with a decimal point.
    [InlineData(".", false)]  // Just a decimal point.
    public void IsInteger(string text, bool isInteger)
    {
        text.IsInteger().Should().Be(isInteger);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1.1, false)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1.00001, false)]
    [InlineData(1.99999, false)]
    [InlineData(0.00001, false)]
    public void DecimalIsInteger(decimal value, bool isInteger)
    {
        value.IsInteger().Should().Be(isInteger);
    }
    [Fact]
    public void SmallDecimalIsInteger()
    {
        1.0000000000000000000001M.IsInteger().Should().Be(false);
    }
    [Fact]
    public void BigDecimalIsInteger()
    {
        1.9999999999999999999999M.IsInteger().Should().Be(false);
    }

    [Theory]
    [InlineData(1.0, true)]
    [InlineData(1.1, false)]
    [InlineData(-1.0, true)]
    [InlineData(0.0, true)]
    [InlineData(double.MaxValue, false)] // double.MaxValue has a fractional part
    [InlineData(double.MinValue, false)] // double.MinValue has a fractional part
    [InlineData(1.00001, false)]
    [InlineData(1.99999, false)]
    [InlineData(0.00001, false)]
    [InlineData(1.0000000000001, false)] // Using values relevant to double's precision
    [InlineData(1.9999999999999, false)]
    public void DoubleIsInteger(double value, bool isInteger)
    {
        value.IsInteger().Should().Be(isInteger);
    }
    [Theory]
    [InlineData(1.0, true)]
    [InlineData(1.1, false)]
    [InlineData(-1.0, true)]
    [InlineData(0.0, true)]
    [InlineData(double.MaxValue, false)] // double.MaxValue has a fractional part
    [InlineData(double.MinValue, false)] // double.MinValue has a fractional part
    [InlineData(1.00001, false)]
    [InlineData(1.99999, false)]
    [InlineData(0.00001, false)]
    [InlineData(1.0000000000001, true)] // what would be good fo double overflows float
    [InlineData(1.9999999999999, true)]
    public void FloatIsInteger(float value, bool isInteger)
    {
        value.IsInteger().Should().Be(isInteger);
    }

    [Theory]
    [InlineData("123", true)]              // Regular integer
    [InlineData("-123", true)]             // Negative integer
    [InlineData("+123", true)]             // Positive integer with explicit sign
    [InlineData("0.123", true)]            // Decimal number
    [InlineData(".123", true)]             // Decimal without leading zero
    [InlineData("123.", true)]             // Decimal without trailing digits
    [InlineData("1.23e-45", true)]         // Decimal with negative exponent
    [InlineData("1.23e45", true)]          // Decimal with positive exponent
    [InlineData("+1.23e45", true)]         // Decimal with explicit positive sign and exponent
    [InlineData("-.123", true)]            // Negative decimal without leading zero
    [InlineData("+.123", true)]            // Positive decimal without leading zero with explicit sign
    [InlineData("0123", false)]            // Leading zero
    [InlineData(".", false)]               // Just a dot
    [InlineData("-.", false)]              // Negative dot
    [InlineData("+.", false)]              // Positive dot
    [InlineData("1.2.3", false)]           // Multiple dots
    [InlineData("1.23e", false)]           // Incomplete scientific notation
    [InlineData("1.23e-", false)]          // Incomplete scientific notation with negative
    [InlineData("abc", false)]             // Non-numeric characters
    public void IsNumeric(string input, bool expected)
    {
        bool result = input.IsNumeric();
        Assert.Equal(expected, result);
    }


    [Theory]
    [InlineData("123", Integer, typeof(long))]
    [InlineData("'123'", Integer, typeof(string))]
    [InlineData("123.456", Real, typeof(double))]
    [InlineData("'123.456'", Real, typeof(string))]
    [InlineData("NotANumber", Integer, typeof(string))]
    [InlineData("123", Text, typeof(string))]
    [InlineData("123", Numeric, typeof(long))]
    [InlineData("123.456", Numeric, typeof(double))]
    [InlineData("'123'", Numeric, typeof(string))]
    [InlineData("2.0", Real, typeof(double))]
    [InlineData("2.0", Numeric, typeof(long))]
    [InlineData("'2.0'", Integer, typeof(string))]
    public void SqliteTypeConversionTheory(string? input, TypeAffinity affinity, Type expectedType)
    {
        var result = input.ConvertToSqliteType(affinity);
        Assert.IsType(expectedType, result);
    }
    [Theory]
    [InlineData("123", Integer)]
    [InlineData("'123'", Text)]
    [InlineData("123.456", Real)]
    [InlineData("'123.456'", Text)]
    [InlineData("x'1234'", Blob)]
    [InlineData("NULL", None)]
    [InlineData("2.0", Real)]
    [InlineData("'2.0'", Text)]
    [InlineData("1.0e3", Real)]
    [InlineData("-123", Integer)]
    [InlineData("-123.456", Real)]
    [InlineData("0", Integer)]
    [InlineData("0.0", Real)]
    [InlineData("-0", Integer)]
    [InlineData("''", Text)]
    [InlineData("' '", Text)]
    [InlineData("x''", Blob)]
    [InlineData("abc", Blob)]
    [InlineData("", Blob)]
    [InlineData(null, None)]
    public void LexicalAffinityTheory(string input, TypeAffinity expectedAffinity)
    {
        Assert.Equal(expectedAffinity, input.GetLexicalAffinity());
    }
}