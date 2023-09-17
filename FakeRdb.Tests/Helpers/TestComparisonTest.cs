using Xunit.Abstractions;
using Xunit.Sdk;

namespace FakeRdb.Tests;

public sealed class TestComparisonTest : ComparisonTests
{
    public TestComparisonTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void AssertErrorsMatch_Should_Check_Regex_Groups()
    {

        AssertErrorsMatch("SQLite Error 1: 'no such table: XXX'.",
            "The given key 'XXX' was not present in the dictionary.");
        Assert.Throws<XunitException>(() =>
            AssertErrorsMatch("SQLite Error 1: 'no such table: XXX'.",
                "The given key 'YYY' was not present in the dictionary."));
    }

}