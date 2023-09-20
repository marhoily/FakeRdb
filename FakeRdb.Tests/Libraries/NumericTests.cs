namespace FakeRdb.Tests.Libraries;

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
}