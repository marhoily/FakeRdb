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
    public void Check(string exp) => 
        _dbPair.QueueForBothDbs($"SELECT {exp}")
            .AssertResultsAreIdentical();
}