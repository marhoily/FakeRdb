namespace FakeRdb.Tests;

public sealed class UnionTests : ComparisonTestBase
{
    public UnionTests(ITestOutputHelper output) : base(output)
    {
        Execute("""
                -- Create tables
                CREATE TABLE City (
                    Name TEXT,
                    Country TEXT,
                    Population INTEGER,
                    Area REAL
                );

                CREATE TABLE Country (
                    Name TEXT,
                    Continent TEXT,
                    Population INTEGER,
                    Area REAL
                );

                -- Insert data into City
                INSERT INTO City (Name, Country, Population, Area) VALUES
                ('New York', 'USA', 8419600, 468.9),
                ('Huston', 'USA', 3980400, 503.0),
                ('Berlin', 'Germany', 3669491, 891.7),
                ('Munich', 'Germany', 1471508, 310.7),
                (NULL, 'Unknown', NULL, NULL),  -- NULL values
                ('New York', 'USA', 8419600, 468.9);  -- Duplicate row

                -- Insert data into Country
                INSERT INTO Country (Name, Continent, Population, Area) VALUES
                ('USA', 'North America', 331420000, 9834000),
                ('Germany', 'Europe', 83166711, 357022),
                ('China', 'Asia', 1444216107, 9597000),
                (NULL, 'Unknown', NULL, NULL),  -- NULL values
                ('USA', 'North America', 331420000, 9834000);  -- Duplicate row

                """);
    }
    [Theory]
    [MemberData(nameof(GetPermutations))]
    public void Union(string combinator, string[] cityColumns, string[] countryColumns )
    {
        CompareAgainstSqlite(
            $"""
            SELECT {string.Join(", ", cityColumns)} FROM City
            {combinator}
            SELECT {string.Join(", ", countryColumns)} FROM Country
            """);
    }
    public static IEnumerable<object[]> GetPermutations()
    {
        // Test each combinator at least once
        yield return new object[] { "UNION", new[] { "Name" }, new[] { "Name" } };
        yield return new object[] { "UNION ALL", new[] { "Name", "Population" }, new[] { "Name", "Population" } };
        yield return new object[] { "INTERSECT", new[] { "Name" }, new[] { "Name" } };
        yield return new object[] { "INTERSECT", new[] { "Country" }, new[] { "Name" } };
        yield return new object[] { "EXCEPT", new[] { "Name" }, new[] { "Name" } };

        // Test column mismatch
        yield return new object[] { "UNION", new[] { "Name" }, new[] { "Population" } };

        // Test different number of columns
        yield return new object[] { "UNION", new[] { "Name" }, new[] { "Name", "Population" } };

        // Test null columns
        yield return new object[] { "UNION", new[] { "Name", "Population" }, new[] { "Name", "Continent" } };

        // Optionally, add more handpicked test cases here...
    }


}