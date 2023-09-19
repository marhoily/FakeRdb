using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class UpdateTests : ComparisonTests
{
    public UpdateTests(ITestOutputHelper output) : base(output)
    {
        Prototype.SeedCustomersOrders();
        Sut.SeedCustomersOrders();
    }

    [Fact]
    public void UpdateEmail()
    {
        AssertReadersMatch("""
                           UPDATE customers
                           SET email = 'new.email@example.com'
                           WHERE customer_id = 1;
                           """);
        AssertReadersMatch("select * from customers");

    }

    [Fact]
    public void UpdateAmount()
    {
        AssertReadersMatch("""
                           UPDATE orders
                           SET total_amount = 300.00
                           WHERE order_id = 2;
                           """);
        AssertReadersMatch("select * from orders");

    }
}