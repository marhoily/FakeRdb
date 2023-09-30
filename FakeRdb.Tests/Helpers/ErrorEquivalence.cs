using System.Text.RegularExpressions;

namespace FakeRdb.Tests;

public static class ErrorEquivalence
{
    private static readonly string[][] ErrorEquivalenceTable =
    {
        new[]
        {
            "SQLite Error 1: 'no such table: (?<t>[A-Za-z.]+)'.",
            "The given key '(?<t>[A-Za-z.]+)' was not present in the dictionary."
        },
        new[]
        {
            "SQLite Error 1: 'no such column: (?<t>[A-Za-z.]+)'.",
            "Column (?<t>[A-Za-z.]+) is not found"
        },
        new[]
        {
            "SQLite Error 1: 'SELECTs to the left and right of UNION do not have the same number of result columns'.",
            "SELECTs to the left and right of UNION do not have the same number of result columns"
        },
        new[]
        {
            "SQLite Error 1: 'ambiguous column name: (?<t>\\w+)'.",
            "Ambiguous column ref: (?<t>\\w+)"
        },
    };
    // Makes sure actual error either matches the expected completely,
    // or equivalent to any of it counterparts in the lookup table, using Regex
    public static void Assert(string expected, string actual)
    {
        if (expected == actual) return;
        var (equivalenceClass, expectedMatch) = ErrorEquivalenceTable
            .Select(errorSet => (errorSet, match: errorSet.FindMatch(expected)))
            .FirstOrDefault(t => t.match != null);
        if (equivalenceClass == null || expectedMatch == null)
        {
            Xunit.Assert.Fail($"""
                               Expected error equivalence class is NOT found!
                               {expected}
                               """);
        }

        var actualMatch = equivalenceClass.FindMatch(actual);
        if (actualMatch == null)
        {
            Xunit.Assert.Fail(
                $"""
                 Actual error message does not fit the equivalence class!

                 Equivalence class:
                 {string.Join("\n", equivalenceClass)}

                 Actual error message:
                 {actual}
                 """);
        }

        var expectedGroups = expectedMatch.Groups
            .Cast<Group>()
            .Where(group => group.Name != "0")
            .Select(group => new { groupName = group.Name, group.Value });

        var actualGroups = actualMatch.Groups
            .Cast<Group>()
            .Where(group => group.Name != "0")
            .Select(group => new { groupName = group.Name, group.Value });

        actualGroups.Should().BeEquivalentTo(expectedGroups,
            options => options.WithStrictOrdering());
    }
}