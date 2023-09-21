
namespace FakeRdb.Tests;

public sealed class TypelessColumnTests : ComparisonTestBase
{
    public TypelessColumnTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void EmptyOutput()
    {
        CompareAgainstSqlite(
            """
             CREATE TABLE T (C);
             INSERT INTO T (C) VALUES (1);
             """, 
            printOut: false);
        CompareAgainstSqlite(
            """
            SELECT * FROM T WHERE C = 3;

            """);
    }

    [Theory]
    [InlineData("integer", "1")]
    [InlineData("text", "'1'")]
    public void Test(string d, string v)
    {
        CompareAgainstSqlite(
            $"""
             CREATE TABLE T (C);
             INSERT INTO T (C) VALUES ({v});
             """, 
            printOut: false,
            description: d);
        CompareAgainstSqlite(
            """
            SELECT * FROM T

            """);
    }
}
