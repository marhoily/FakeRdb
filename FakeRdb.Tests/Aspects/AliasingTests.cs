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
    [InlineData("Alias in ORDER BY clause",
        "SELECT title AS alias FROM Album ORDER BY alias")]
    [InlineData("Table alias", "SELECT x.year FROM Album as x")]
    public void F(string d, string sql)
    {
        CompareAgainstSqlite(sql, d);
    }
}