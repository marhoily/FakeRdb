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

    [Fact]
    public void ColumnName_In_RValue()
    {
        AssertReadersMatch("""
                           UPDATE orders
                           SET total_amount = total_amount * 1.10; -- Increase all order totals by 10%
                           """);
        AssertReadersMatch("select * from orders");
    }
    [Fact]
    public void Update_Multiple_Fields()
    {
        AssertReadersMatch("""
                           UPDATE customers
                           SET customer_name = 'Johnny Doe', email = 'johnny.doe@example.com'
                           WHERE customer_id = 1;
                           """);
        AssertReadersMatch("select * from orders");
    }

}