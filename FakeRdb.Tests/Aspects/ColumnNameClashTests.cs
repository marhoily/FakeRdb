namespace FakeRdb.Tests;

public sealed class ColumnNameClashTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public ColumnNameClashTests(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output)
            .ExecuteOnBoth(
            """
                CREATE TABLE Country (Name TEXT);

                INSERT INTO Country (Name) VALUES ('USA');
                INSERT INTO Country (Name) VALUES ('Germany');
                INSERT INTO Country (Name) VALUES ('Japan');

                CREATE TABLE City (Name TEXT);

                INSERT INTO City (Name) VALUES ('New York');
                INSERT INTO City (Name) VALUES ('Berlin');
                """);
    }
    
    [Fact]
    public void Should_Return_2x3_Rows()
    {
        _dbPair.QueueForBothDbs("SELECT * FROM Country, City")
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Ambiguous_Column_Name_In_Where()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM Country, City
            WHERE Name = 'USA' AND Name = 'Berlin'
            """)
            .Anticipate(Outcome.Error)
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Ambiguous_Column_Name_In_Select()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT Name FROM Country, City
            """)
            .Anticipate(Outcome.Error)
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Disambiguate_Column_Name_In_Select()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT Country.Name, City.Name FROM Country, City
            """).AssertResultsAreIdentical();
    }
    [Fact]
    public void Disambiguate_Column_Name_In_Where()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM Country, City
            WHERE Country.Name = 'USA' AND Country.Name = 'Berlin'
            """).AssertResultsAreIdentical();
    }
    [Fact]
    public void Cross_Reference()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM Country
            UNION
            SELECT Country.Name FROM City
            """)
            .Anticipate(Outcome.Error)
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Alias_Cross_Reference()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT * FROM Country as x
            UNION
            SELECT x.Name FROM City
            """)
            .Anticipate(Outcome.Error)
            .AssertResultsAreIdentical();
    }
}