using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class SelectTests : ComparisonTestBase
{
    public SelectTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }

    [Theory]
    [InlineData("Table not found", "SELECT * FROM Wrong")]
    [InlineData("Select every column", "SELECT * FROM Album")]
    [InlineData("Select one column", "SELECT Title FROM Album")]
    [InlineData("Select a wrong column", "SELECT Wrong FROM Album")]
    [InlineData("Select a case-sensitive column", "SELECT title FROM Album")]
    [InlineData("Escape using []", "SELECT [title] FROM [Album]")]
    [InlineData("Escape using ``", "SELECT `title` FROM `Album`")]
    [InlineData("Use column name in the right value of an expression", "select year+ 1 from Album")]
    [InlineData("Binary operation in select", "select 1 +1 from Album")]
    [InlineData("Floating number with a dot in select", "select 1.000 from Album")]
    [InlineData("Number with exponent in select", "select 1e-3 from Album")]
    [InlineData("Decimal without trailing digits in select", "select 123. from Album")]
    [InlineData("Integer in select", "select 12 from Album")]
    [InlineData("Text in select", "select '12' from Album")]
    public void F(string d, string sql)
    {
        CompareAgainstSqlite(sql, d);
    }

}