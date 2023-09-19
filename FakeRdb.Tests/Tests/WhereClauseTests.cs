using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class WhereClauseTests : ComparisonTests
{
    public WhereClauseTests (ITestOutputHelper output) : base(output)
    {
    }
    
    [Fact]
    public void LessThan()
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
        CompareAgainstSqlite("SELECT * FROM Album WHERE Year < 2023");
    }
}