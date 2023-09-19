using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class InsertTests : ComparisonTests
{
    public InsertTests(ITestOutputHelper output) : base(output)
    {
        Prototype.Seed3Albums();
        Sut.Seed3Albums();
    }
    
    [Fact]
    public void Insert_Expression()
    {
        AssertReadersMatch(
            "INSERT INTO Album (Title, Artist, Year) " +
            "VALUES ('blah', 'blah', 1+1)");
    }
}