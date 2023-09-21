namespace FakeRdb.Tests;

public sealed class OrderByTests : ComparisonTestBase
{
    public OrderByTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedHeterogeneousData();
        Sut.SeedHeterogeneousData();
    }

    [Theory]
    [InlineData("Order by", "SELECT * FROM T ORDER BY X")]
    public void F(string d, string sql)
    {
        CompareAgainstSqlite(sql, d);
    }

}