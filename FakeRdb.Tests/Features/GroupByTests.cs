﻿namespace FakeRdb.Tests;

public sealed class GroupByTests : ComparisonTestBase
{
    private readonly DbPair _dbPair;

    public GroupByTests(ITestOutputHelper output)
    {
        _dbPair = new DbPair(SqliteConnection, SutConnection)
            .LogQueryAndResultsTo(output)
            .ExecuteOnBoth(
            """
            CREATE TABLE City (
                Name TEXT,
                Country TEXT,
                Population INTEGER,
                Area REAL
            );

            INSERT INTO City (Name, Country, Population, Area) VALUES
            ('New York', 'USA', 8419600, 468.9),
            ('Los Angeles', 'USA', 3980400, 503),
            ('Chicago', 'USA', 2716000, 589),
            ('Houston', 'USA', 2328000, 671),
            ('Berlin', 'Germany', 3669000, 891.8),
            ('Munich', 'Germany', 1472000, 310.4),
            ('Hamburg', 'Germany', 1845000, 755.2),
            ('London', 'UK', 8982000, 1572),
            ('Birmingham', 'UK', 1131200, 267.77),
            ('Glasgow', 'UK', 626410, 175.5),
            ('Tokyo', 'Japan', 13929000, 2191),
            ('Osaka', 'Japan', 2666000, 552.3),
            ('Nagoya', 'Japan', 2296000, 326.45),
            ('Delhi', 'India', 16790000, 1484),
            ('Mumbai', 'India', 12442000, 603.4);
            """);
    }
    [Fact]
    public void GroupByCountry()
    {
        _dbPair.QueueForBothDbs(
            "SELECT Sum(Area) FROM City GROUP BY Country")
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Should_Filter_Before_GroupBy()
    {
        _dbPair.QueueForBothDbs(
            "SELECT Avg(Area) FROM City WHERE Country = 'USA'")
            .AssertResultsAreIdentical();
    }
    [Fact]
    public void Multiple_Aggregate_Functions()
    {
        _dbPair.QueueForBothDbs(
            """
            SELECT Country, 
                Sum(Population) as X, 
                AVG(Area) as Y, 
                MAX(Population) as Z
            FROM City 
            GROUP BY Country
            """)
            .AssertResultsAreIdentical();
    }
}