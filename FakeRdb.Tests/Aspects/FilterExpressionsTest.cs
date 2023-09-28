namespace FakeRdb.Tests;

public sealed class FilterExpressionsTest : ComparisonTestBase
{
    public FilterExpressionsTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void Compound_Binary()
    {
        Sqlite.SeedCustomersOrders();
        Sut.SeedCustomersOrders();

        CompareAgainstSqlite(
            """
            SELECT * FROM orders, customers
            WHERE orders.customer_id + customers.customer_id = 5
            """);
    }

    [Fact]
    public void Double_And()
    {
        ExecuteOnBoth(
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

        CompareAgainstSqlite(
            """
            SELECT id FROM Questionnaire
            WHERE IsAdult AND IsEmployed AND HasCar
            """);
    }
}