namespace FakeRdb.Tests;

public sealed class AliasingTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public AliasingTests(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output)
            .ExecuteOnBoth(DbSeed.Albums);
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
    public void Check(string d, string sql)
    {
        _dbPair.WithName(d)
            .QueueForBothDbs(sql)
            .AssertResultsAreIdentical();
    }
}