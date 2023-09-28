namespace FakeRdb.Tests;

public sealed class FilterExpressionsTest : ComparisonTestBase
{
    public FilterExpressionsTest(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedCustomersOrders();
        Sut.SeedCustomersOrders();
    }

    [Fact]
    public void Compound_Binary()
    {
        CompareAgainstSqlite(
            """
            SELECT * FROM orders, customers
            WHERE orders.customer_id + customers.customer_id = 5
            """);
    }
}