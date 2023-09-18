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
}