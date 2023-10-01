namespace FakeRdb.Tests;

public sealed class CartesianProductTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public CartesianProductTests(ITestOutputHelper helper)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(helper)
            .ExecuteOnBoth(
                """
                CREATE TABLE Country (CountryName TEXT);
                CREATE TABLE City (CityName TEXT);
                
                INSERT INTO Country VALUES 
                    ('USA'), ('Germany'), ('Japan');

                INSERT INTO City VALUES
                    ('New York'), ('Berlin');
                """);
    }

    [Fact]
    public void Should_Return_2x3_Rows()
    {
        _dbPair.QueueForBothDbs("SELECT * FROM Country, City")
            .AssertResultsAreIdentical(cfg => cfg.IncludeQueryPlan());
    }
    [Fact]
    public void Where_References_Different_Tables()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM Country, City
            WHERE CountryName = 'USA' AND CityName = 'Berlin'
            """)
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Effective_Join()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM Country, City
            WHERE CountryName = CityName
            """)
            .AssertResultsAreIdentical();
    }
}