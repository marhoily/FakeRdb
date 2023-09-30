namespace FakeRdb.Tests;

public sealed class UpdateTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public UpdateTests(ITestOutputHelper output) 
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .ExecuteOnBoth(DbSeed.CustomersAndOrders)
            .LogQueryAndResultsTo(output);
    }

    [Fact]
    public void UpdateEmail()
    {
        _dbPair.ExecuteOnBoth("""
                              UPDATE customers
                              SET email = 'new.email@example.com'
                              WHERE customer_id = 1;
                              """);
        _dbPair.QueueForBothDbs("select * from customers")
            .AssertResultsAreIdentical();
    }

    [Fact]
    public void UpdateAmount()
    {
        _dbPair.ExecuteOnBoth("""
                              UPDATE orders
                              SET total_amount = 300.00
                              WHERE order_id = 2;
                              """);
        _dbPair.QueueForBothDbs("select * from orders")
            .AssertResultsAreIdentical();

    }

    [Fact]
    public void ColumnName_In_RValue()
    {
        _dbPair.ExecuteOnBoth("""
                              UPDATE orders
                              SET total_amount = total_amount * 1.10; -- Increase all order totals by 10%
                              """);
        _dbPair.QueueForBothDbs("select * from orders")
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Update_Multiple_Fields()
    {
        _dbPair.ExecuteOnBoth("""
                              UPDATE customers
                              SET customer_name = 'Johnny Doe', email = 'johnny.doe@example.com'
                              WHERE customer_id = 1;
                              """);
        _dbPair.QueueForBothDbs("select * from orders")
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Based_On_Join_Condition()
    {
        _dbPair.ExecuteOnBoth(
            """
            UPDATE customers
            SET email = 'updated.email@example.com'
            WHERE customer_id IN (
                SELECT customer_id FROM orders 
                WHERE order_date < '2023-09-05');
            """);
        _dbPair.QueueForBothDbs("select * from orders")
            .AssertResultsAreIdentical();
    }
}