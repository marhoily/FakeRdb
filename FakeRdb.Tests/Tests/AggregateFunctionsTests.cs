using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class TypeAffinityTests : ComparisonTests
{
    public TypeAffinityTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedColumnAffinityTable();
        Sut.SeedColumnAffinityTable();
    }
    [Fact]
    public void Insert_Text()
    {
        CompareAgainstSqlite(
            """
            -- Values stored as TEXT, INTEGER, INTEGER, REAL, TEXT.
            INSERT INTO t1 VALUES('500.0', '500.0', '500.0', '500.0', '500.0');
            SELECT typeof(t), typeof(nu), typeof(i), typeof(r), typeof(no) FROM t1;
            -- text|integer|integer|real|text
            """);
    }
}
public sealed class AggregateFunctionsTests : ComparisonTests
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