using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class AliasingTests : ComparisonTestBase
{
    public AliasingTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }

    [Theory]
   // [InlineData("Cannot alias *", "SELECT * as x FROM Album")]
    [InlineData("Column alias", "SELECT year as x FROM Album")]
    [InlineData("Alias reference", "SELECT year as x, x+1 FROM Album")] // Error
    [InlineData("Alias binary expression", 
        """SELECT Title || ' ' || Artist as "Caption" FROM Album""")]
    public void F(string d, string sql)
    {
        CompareAgainstSqlite(sql, d);
    }

}