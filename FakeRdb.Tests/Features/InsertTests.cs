namespace FakeRdb.Tests;

public sealed class InsertTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public InsertTests(ITestOutputHelper output) 
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output)
            .ExecuteOnBoth(DbSeed.Albums);
    }

    [Fact]
    public void Insert_Expression()
    {
        _dbPair.QueueForBothDbs(
            "INSERT INTO Album (Title, Artist, Year) " +
            "VALUES ('blah', 'blah', 1+1)")
            .AssertResultsAreIdentical();
    }
}