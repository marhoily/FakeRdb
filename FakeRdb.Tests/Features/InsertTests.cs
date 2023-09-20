using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class InsertTests : ComparisonTestBase
{
    public InsertTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }

    [Fact]
    public void Insert_Expression()
    {
        CompareAgainstSqlite(
            "INSERT INTO Album (Title, Artist, Year) " +
            "VALUES ('blah', 'blah', 1+1)");
    }
}