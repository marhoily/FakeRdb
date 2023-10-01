namespace FakeRdb.Tests;

public sealed class ConditionAnalysisTest : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public ConditionAnalysisTest(ITestOutputHelper output) 
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output);
    }

    [Theory]
    [InlineData("false = NULL")]
    [InlineData("orders.order_id = 4")]
    [InlineData("4 = orders.order_id")]
    [InlineData("orders.customer_id + customers.customer_id = 5")]
    [InlineData("5 = orders.customer_id + customers.customer_id")]
    [InlineData("orders.customer_id * 2 = orders.order_id")]
    [InlineData("orders.order_id = orders.customer_id * 2")]
    [InlineData("orders.customer_id * 2 = customers.customer_id")]
    [InlineData("customers.customer_id = orders.customer_id * 2")]
    [InlineData("orders.customer_id * 2 = 4")]
    [InlineData("4 = orders.customer_id * 2")]
    [InlineData("(4 = orders.customer_id) * 2")]
    [InlineData("orders.customer_id * 2 = 2 * orders.customer_id")]
    public void Check(string filter)
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair
            .QueueForBothDbsWithArgs(
            $"""
            SELECT customer_name FROM orders, customers
            WHERE {filter}
            """)
            .AssertResultsAreIdentical(cfg => cfg.IncludeQueryPlan());
    }

    [Theory]
    [InlineData("orders.customer_id * 2 = @p", 4)]
    [InlineData("@p = orders.customer_id * 2", 4)]
    public void WithBinding(string filter, object? parameter)
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair
            .QueueForBothDbsWithArgs(
                $"""
                 SELECT customer_name FROM orders, customers
                 WHERE {filter}
                 """, ("@p", parameter))
            .AssertResultsAreIdentical();
    }

    [Fact]
    public void Double_And()
    {
        _dbPair.ExecuteOnBoth(
            """
            CREATE TABLE Questionnaire (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                IsAdult INTEGER,
                IsEmployed INTEGER,
                HasCar INTEGER,
                IsMarried INTEGER,
                HasPets INTEGER
            );

            INSERT INTO Questionnaire (IsAdult, IsEmployed, HasCar, IsMarried, HasPets) VALUES
            (1, 1, 1, 1, 0),
            (1, 1, 0, 0, 1),
            (0, 0, 0, 0, 1),
            (1, 0, 1, 0, 1),
            (1, 1, 1, 1, 1),
            (0, 0, 0, 0, 0),
            (1, 0, 0, 1, 0),
            (1, 1, 0, 1, 1);
            """);

        _dbPair.QueueForBothDbs(
            """
            SELECT id FROM Questionnaire
            WHERE IsAdult < IsEmployed AND HasCar
            """).AssertResultsAreIdentical();
    }
}