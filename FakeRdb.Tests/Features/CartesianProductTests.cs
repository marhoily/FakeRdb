namespace FakeRdb.Tests;

public sealed class CartesianProductTests : ComparisonTestBase
{
    public CartesianProductTests(ITestOutputHelper output) : base(output)
    {
        ExecuteOnBoth(
            """
            CREATE TABLE Country (CountryName TEXT);

            INSERT INTO Country (CountryName) VALUES ('USA');
            INSERT INTO Country (CountryName) VALUES ('Germany');
            INSERT INTO Country (CountryName) VALUES ('Japan');

            CREATE TABLE City (CityName TEXT);

            INSERT INTO City (CityName) VALUES ('New York');
            INSERT INTO City (CityName) VALUES ('Berlin');
            """);
    }
    
    [Fact]
    public void Should_Return_2x3_Rows()
    {
        CompareAgainstSqlite("SELECT * FROM Country, City");
    }
}