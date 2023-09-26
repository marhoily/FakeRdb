namespace FakeRdb.Tests;

public sealed class ColumnNameClashTests : ComparisonTestBase
{
    public ColumnNameClashTests(ITestOutputHelper output) : base(output)
    {
        ExecuteOnBoth(
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
        CompareAgainstSqlite("SELECT * FROM Country, City");
    }
    [Fact]
    public void Ambiguous_Column_Name_In_Where()
    {
        CompareAgainstSqlite(
            """
            SELECT * FROM Country, City
            WHERE Name = 'USA' AND Name = 'Berlin'
            """);
    }
    [Fact]
    public void Ambiguous_Column_Name_In_Select()
    {
        CompareAgainstSqlite(
            """
            SELECT Name FROM Country, City
            """);
    }
    [Fact]
    public void Disambiguate_Column_Name_In_Select()
    {
        CompareAgainstSqlite(
            """
            SELECT Country.Name, City.Name FROM Country, City
            """);
    }
}