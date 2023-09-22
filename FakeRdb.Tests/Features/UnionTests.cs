namespace FakeRdb.Tests;

public sealed class UnionTests : ComparisonTestBase
{
    public UnionTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }
    [Fact]
    public void Max()
    {
        CompareAgainstSqlite(
            """
            SELECT Title AS TitleOrArtist, Year
            FROM Album
            UNION ALL
            SELECT Artist AS TitleOrArtist, Year 
            FROM Album
            """);
    }
}