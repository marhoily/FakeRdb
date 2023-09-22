namespace FakeRdb.Tests;

public sealed class DeleteTests : ComparisonTestBase
{
    public DeleteTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }

    [Fact]
    public void Delete_All()
    {
        CompareAgainstSqlite(
            "DELETE FROM Album; " +
            "SELECT * FROM Album");
    }

    [Fact]
    public void Delete_Predicated()
    {
        CompareAgainstSqlite(
            "DELETE FROM Album WHERE Year = 2021; " +
            "SELECT * FROM Album");
    }
}