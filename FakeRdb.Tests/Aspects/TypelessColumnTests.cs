
namespace FakeRdb.Tests;

public sealed class TypelessColumnTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public TypelessColumnTests(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output);
    }

    [Fact]
    public void EmptyOutput()
    {
        _dbPair.ExecuteOnBoth(
            """
             CREATE TABLE T (C);
             INSERT INTO T (C) VALUES (1);
             """);
        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM T WHERE C = 3;

            """)
            .AssertResultsAreIdentical();
    }

    [Theory]
    [InlineData("integer", "1")]
    [InlineData("text", "'1'")]
    public void Test(string d, string v)
    {
        _dbPair.ExecuteOnBoth(
            $"""
             CREATE TABLE T (C);
             INSERT INTO T (C) VALUES ({v});
             """)
            .WithName(d);
        _dbPair.QueueForBothDbs("SELECT * FROM T")
            .AssertResultsAreIdentical();
    }
}
