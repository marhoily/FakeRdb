namespace FakeRdb.Tests;

public sealed class ConditionAnalysisTest : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public ConditionAnalysisTest(ITestOutputHelper output) 
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output);
    }

    [Fact]
    public void Compound_Binary()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM orders, customers
            WHERE orders.customer_id + customers.customer_id = 5
            """).AssertResultsAreIdentical();
    }

    [Fact]
    public void Const_Expr()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM orders
            WHERE false = NULL
            """).AssertResultsAreIdentical();
    }

    [Fact]
    public void NonEquiJoin_With_Function_On_One_Side()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM orders, customers
            WHERE orders.customer_id * 2 = customers.customer_id
            """).AssertResultsAreIdentical();
    }

    [Fact]
    public void SingleTable_Rhs()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM orders
            WHERE 4 = orders.order_id
            """).AssertResultsAreIdentical();
    }
    [Fact]
    public void SingleTable_With_Subexpression()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT customer_name FROM orders, customers
            WHERE orders.customer_id * 2 = orders.order_id
            """).AssertResultsAreIdentical();
    }
    [Fact]
    public void SingleTable_With_Subexpression_And_Binding()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbsWithArgs(
            """
            SELECT customer_name FROM orders, customers
            WHERE orders.customer_id * 2 = @p1
            """, ("@p1", 4)).AssertResultsAreIdentical();
    }
    [Fact]
    public void SingleTable_Lhs_Sandwich()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT customer_name FROM orders, customers
            WHERE orders.customer_id * 2 = 4
            """).AssertResultsAreIdentical();
    }
    [Fact]
    public void SingleTable_Rhs_Sandwich()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT customer_name FROM orders, customers
            WHERE 4 = orders.customer_id * 2
            """).AssertResultsAreIdentical();
    }
    [Fact]
    public void SingleTable_Rhs_Sandwich_Equals_First()
    {
        _dbPair.ExecuteOnBoth(DbSeed.CustomersAndOrders);

        _dbPair.QueueForBothDbs(
            """
            SELECT customer_name FROM orders, customers
            WHERE (4 = orders.customer_id) * 2
            """).AssertResultsAreIdentical();
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