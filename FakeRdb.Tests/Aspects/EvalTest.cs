namespace FakeRdb.Tests;

public sealed class EvalTest : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public EvalTest(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output);
    }

    [Theory]
    [InlineData("true < 2")]
    [InlineData("'str' AND 15")]
    //[InlineData("CAST('5e2' as bool)")]
    [InlineData("'5e2' OR false")]
    [InlineData("'5e2' OR 'true'")]
    public void Check(string exp) => 
        _dbPair.QueueForBothDbs($"SELECT {exp}")
            .AssertResultsAreIdentical();
    [Fact]
    public void QueryPlan() => 
        _dbPair.QueueForBothDbs("SELECT 1")
            .AssertResultsAreIdentical(cfg => cfg.IncludeQueryPlan());
}