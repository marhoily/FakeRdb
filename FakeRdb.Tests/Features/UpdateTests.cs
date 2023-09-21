namespace FakeRdb.Tests;

public sealed class UpdateTests : ComparisonTestBase
{
    public UpdateTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedCustomersOrders();
        Sut.SeedCustomersOrders();
    }

    [Fact]
    public void UpdateEmail()
    {
        CompareAgainstSqlite("""
                           UPDATE customers
                           SET email = 'new.email@example.com'
                           WHERE customer_id = 1;
                           """);
        CompareAgainstSqlite("select * from customers");
    }

    [Fact]
    public void UpdateAmount()
    {
        CompareAgainstSqlite("""
                           UPDATE orders
                           SET total_amount = 300.00
                           WHERE order_id = 2;
                           """);
        CompareAgainstSqlite("select * from orders");

    }

    [Fact]
    public void ColumnName_In_RValue()
    {
        CompareAgainstSqlite("""
                           UPDATE orders
                           SET total_amount = total_amount * 1.10; -- Increase all order totals by 10%
                           """);
        CompareAgainstSqlite("select * from orders");
    }
    [Fact]
    public void Update_Multiple_Fields()
    {
        CompareAgainstSqlite("""
                           UPDATE customers
                           SET customer_name = 'Johnny Doe', email = 'johnny.doe@example.com'
                           WHERE customer_id = 1;
                           """);
        CompareAgainstSqlite("select * from orders");
    }
    [Fact]
    public void Based_On_Join_Condition()
    {
        CompareAgainstSqlite("""
                           UPDATE customers
                           SET email = 'updated.email@example.com'
                           WHERE customer_id IN (SELECT customer_id FROM orders WHERE order_date < '2023-09-05');
                           """);
        CompareAgainstSqlite("select * from orders");
    }
}