namespace FakeRdb.Tests;

public sealed class AggregateFunctionsTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public AggregateFunctionsTests(ITestOutputHelper helper)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(helper)
            .ExecuteOnBoth(DbSeed.CustomersAndOrders);
    }

    [Theory]
    [InlineData("MAX")]
    [InlineData("Min")]
    [InlineData("avg")]
    public void Check(string functionName)
    {
        _dbPair.QueueForBothDbs(
                $"SELECT {functionName}(total_amount) FROM orders")
            .AssertResultsAreIdentical();
    }
}