using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class MultiTableSelectTests : ComparisonTests
{
    public MultiTableSelectTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedCustomersOrders();
        Sut.SeedCustomersOrders();
    }

    [Fact]
    public void Should_Fill_In_The_Tables()
    {
        CompareAgainstSqlite("select * from customers");
        CompareAgainstSqlite("select * from orders");
    }
}