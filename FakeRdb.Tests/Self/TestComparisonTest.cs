using Xunit.Sdk;

namespace FakeRdb.Tests;

public sealed class TestComparisonTest : ComparisonTestBase
{
    [Fact]
    public void AssertErrorsMatch_Should_Check_Regex_Groups()
    {
        ErrorEquivalence.Assert("SQLite Error 1: 'no such table: XXX'.",
            "The given key 'XXX' was not present in the dictionary.");
        Assert.Throws<XunitException>(() =>
            ErrorEquivalence.Assert("SQLite Error 1: 'no such table: XXX'.",
                "The given key 'YYY' was not present in the dictionary."));
    }

}