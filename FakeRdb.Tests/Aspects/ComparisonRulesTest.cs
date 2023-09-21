namespace FakeRdb.Tests;

public sealed class ComparisonRulesTest
{
    [Theory]
    [InlineData(null, null, 0)]
    [InlineData(null, 1L, -1)]
    [InlineData(1L, null, 1)]
    [InlineData(1L, 2L, -1)]
    [InlineData(2L, 1L, 1)]
    [InlineData(1L, 1L, 0)]
    [InlineData(1.0, 1.1, -1)]
    [InlineData(1.1, 1.0, 1)]
    [InlineData(1.0, 1.0, 0)]
    [InlineData("a", "b", -1)]
    [InlineData("b", "a", 1)]
    [InlineData("a", "a", 0)]
    [InlineData(new byte[] { 1 }, new byte[] { 1, 2 }, -1)]
    [InlineData(new byte[] { 1, 2 }, new byte[] { 1 }, 1)]
    [InlineData(new byte[] { 1 }, new byte[] { 1 }, 0)]
    [InlineData(1L, 2.0, -1)]
    [InlineData(1L, 1.0, 0)]
    [InlineData(2L, 1.0, 1)]
    [InlineData(1.0, "a", -1)]
    [InlineData("a", new byte[] { 1 }, -1)]
    [InlineData(new byte[] { 1 }, 1L, 1)]
    public void CompareSqliteObjectsTheory(object? a, object? b, int expectedResult)
    {
        int result = ComparisonRules.CompareSqliteObjects(a, b);
        Assert.Equal(expectedResult, result);
    }
}