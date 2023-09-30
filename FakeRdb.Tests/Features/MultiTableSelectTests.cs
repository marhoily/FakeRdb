namespace FakeRdb.Tests;

public sealed class MultiTableSelectTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public MultiTableSelectTests(ITestOutputHelper output) 
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output);
    }

    [Fact]
    public void Should_Fill_In_The_Tables()
    {
        _dbPair.QueueForBothDbs("select * from orders, customers")
            .Anticipate(Outcome.Error)
            .AssertResultsAreIdentical();
    }
}