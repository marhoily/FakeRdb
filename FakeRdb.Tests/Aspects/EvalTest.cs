namespace FakeRdb.Tests;

public sealed class EvalTest : ComparisonTestBase
{
    public EvalTest(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("true < 2")]
    public void Check(string exp) => 
        CompareAgainstSqlite($"SELECT {exp}");
}