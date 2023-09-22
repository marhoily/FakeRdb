namespace FakeRdb.Tests;

public sealed class UnionTests : ComparisonTestBase
{
    public UnionTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }
    [Fact]
    public void Union()
    {
        CompareAgainstSqlite(
            """
            SELECT Title, Year
            FROM Album
            UNION ALL
            SELECT Artist, Year 
            FROM Album
            """);
    }
}