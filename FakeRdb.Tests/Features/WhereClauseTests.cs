namespace FakeRdb.Tests;

public sealed class WhereClauseTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public WhereClauseTests (ITestOutputHelper output) 
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output)
            .ExecuteOnBoth(DbSeed.Albums);
    }
    
    [Fact]
    public void LessThan()
    {
        _dbPair.QueueForBothDbs(
            "SELECT * FROM Album WHERE Year < 2023")
            .AssertResultsAreIdentical();
    }
}