namespace FakeRdb.Tests;

public sealed class TypeAffinityTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public TypeAffinityTests(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output);
    }
    

    [Theory]
    [InlineData("TEXT", "'500.0'")]
    [InlineData("TEXT", "'88'")]
    [InlineData("TEXT", "'str'")]
    [InlineData("TEXT", "33")]
    [InlineData("TEXT", "22.0")]
    [InlineData("TEXT", "x'AABB'")]
    [InlineData("TEXT", "NULL")]

    [InlineData("NUMERIC", "'500.0'")]
    [InlineData("NUMERIC", "'88'")]
    [InlineData("NUMERIC", "'str'")]
    [InlineData("NUMERIC", "33")]
    [InlineData("NUMERIC", "22.0")]
    [InlineData("NUMERIC", "x'AABB'")]
    [InlineData("NUMERIC", "NULL")]

    [InlineData("INTEGER", "'500.0'")]
    [InlineData("INTEGER", "'88'")]
    [InlineData("INTEGER", "'str'")]
    [InlineData("INTEGER", "33")]
    [InlineData("INTEGER", "22.0")]
    [InlineData("INTEGER", "x'AABB'")]
    [InlineData("INTEGER", "NULL")]

    [InlineData("REAL", "'500.0'")]
    [InlineData("REAL", "'88'")]
    [InlineData("REAL", "'str'")]
    [InlineData("REAL", "33")]
    [InlineData("REAL", "22.0")]
    [InlineData("REAL", "x'AABB'")]
    [InlineData("REAL", "NULL")]

    [InlineData("BLOB", "'500.0'")]
    [InlineData("BLOB", "'88'")]
    [InlineData("BLOB", "'str'")]
    [InlineData("BLOB", "33")]
    [InlineData("BLOB", "22.0")]
    [InlineData("BLOB", "x'AABB'")]
    [InlineData("BLOB", "NULL")]
    public void Check(string type, string value)
    {
        _dbPair.QueueForBothDbs(
            $"""
            CREATE TABLE tbl (column {type});
            INSERT INTO tbl VALUES({value});
            SELECT *, typeof(column) FROM tbl;
            """)
            .AssertResultsAreIdentical();
    }
}