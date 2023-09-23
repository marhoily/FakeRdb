namespace FakeRdb.Tests;

public sealed class MultiTableSelectTests : ComparisonTestBase
{
    public MultiTableSelectTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedCustomersOrders();
        Sut.SeedCustomersOrders();
    }

    [Fact]
    public void Should_Fill_In_The_Tables()
    {
        CompareAgainstSqlite("select * from orders, customers");
    }
}