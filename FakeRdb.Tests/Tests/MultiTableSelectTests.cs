using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class MultiTableSelectTests : ComparisonTests
{
    public MultiTableSelectTests(ITestOutputHelper output) : base(output)
    {
        Prototype.SeedCustomersOrders();
        Sut.SeedCustomersOrders();
    }

    [Fact]
    public void Should_Fill_In_The_Tables()
    {
        AssertReadersMatch("select * from customers");
        AssertReadersMatch("select * from orders");
    }
}