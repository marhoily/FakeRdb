namespace FakeRdb.Tests;

public sealed class AggregateFunctionsTests : ComparisonTestBase
{
    public AggregateFunctionsTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedCustomersOrders();
        Sut.SeedCustomersOrders();
    }

    [Fact]
    public void Max()
    {
        CompareAgainstSqlite("SELECT MAX(total_amount) FROM orders");
    }
    [Fact]
    public void Min()
    {
        CompareAgainstSqlite("SELECT Min(order_date) FROM orders");
    }
}