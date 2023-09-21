using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class TypelessColumnTests : ComparisonTestBase
{
    public TypelessColumnTests(ITestOutputHelper output) : base(output)
    {
    }
    [Theory]
    [InlineData("integer", "1")]
    public void Test(string d, string v)
    {
        CompareAgainstSqlite(
            $"""
             CREATE TABLE T (C);
             INSERT INTO T (C)
             VALUES ({v});
             """, 
            printOut: false,
            description: d);
        CompareAgainstSqlite(
            """
            SELECT * FROM T;

            """);
    }
}