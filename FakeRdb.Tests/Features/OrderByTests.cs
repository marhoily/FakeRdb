namespace FakeRdb.Tests;

public sealed class OrderByTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public OrderByTests(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output)
            .ExecuteOnBoth(
            """
                CREATE TABLE T (X,Y,Z);
                INSERT INTO T (X, Y, Z)
                VALUES
                    ('Banana', 5, '2022-01-10'),
                    (12345, 'Apple', x'4D414E47'),   -- x'4D414E47' is a BLOB literal for 'MANG'
                    ('Orange', 8.23, 'This is text.'),
                    (98765, NULL, 999),
                    (3.1415, 'Another text', x'ABCDE12345');
                """);
    }

    [Theory]
    [InlineData("Mix text and numbers", "SELECT * FROM T ORDER BY X")]
    [InlineData("NULL", "SELECT * FROM T ORDER BY Y")]
    [InlineData("BLOBS", "SELECT * FROM T ORDER BY Z")]
    public void ShouldSortDataCorrectly(string d, string sql)
    {
        _dbPair
            .WithName(d)
            .QueueForBothDbs(sql)
            .AssertResultsAreIdentical();
    }
}