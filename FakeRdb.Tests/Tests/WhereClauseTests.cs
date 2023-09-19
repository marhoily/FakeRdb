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
        Prototype.Seed3Albums();
        Sut.Seed3Albums();
        AssertReadersMatch("SELECT * FROM Album WHERE Year < 2023");
    }
}