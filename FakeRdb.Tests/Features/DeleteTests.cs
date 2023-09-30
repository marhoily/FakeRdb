namespace FakeRdb.Tests;

public sealed class DeleteTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public DeleteTests(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output)
            .ExecuteOnBoth(DbSeed.Albums);
    }

    [Fact]
    public void Delete_All()
    {
        _dbPair.QueueForBothDbs(
            "DELETE FROM Album; " +
            "SELECT * FROM Album")
            .AssertResultsAreIdentical();
    }

    [Fact]
    public void Delete_Predicated()
    {
        _dbPair.QueueForBothDbs(
            "DELETE FROM Album WHERE Year = 2021; " +
            "SELECT * FROM Album")
            .AssertResultsAreIdentical();
    }
}