using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class WhereClauseTests : ComparisonTests
{
    public WhereClauseTests (ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }
    
    [Fact]
    public void LessThan()
    {
        CompareAgainstSqlite("SELECT * FROM Album WHERE Year < 2023");
    }
}