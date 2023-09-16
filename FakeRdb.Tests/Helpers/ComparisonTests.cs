using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace FakeRdb.Tests;

public abstract class ComparisonTests : IDisposable
{
    protected DbConnection Prototype { get; } = new SqliteConnection("Data Source=:memory:");
    protected DbConnection Sut { get; } = new FakeDbConnection(new FakeDb());

    protected ComparisonTests()
    {
        Prototype.Open();
        Sut.Open();
    }

    public void Dispose()
    {
        Prototype.Dispose();
        Sut.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void AssertReadersMatch(string sql)
    {
        var cmd1 = Prototype.CreateCommand();
        cmd1.CommandText = sql;
        var (reader, x1) = cmd1.SafeExecuteReader();

        var cmd2 = Sut.CreateCommand();
        cmd2.CommandText = sql;
        var (result, x2) = cmd2.SafeExecuteReader();
        if (x1 != null)
        {
            x2.Should().NotBeNull();
            AssertErrorsMatch(x1.Message, x2!.Message);
        }
        else
        {
            reader!.ShouldEqual(result!);
        }
    }

    private static readonly string[][] ErrorEquivalenceTable =
    {
        new[]
        {
            "SQLite Error 1: 'no such table: (?<t>\\w+)'.",
            "The given key '(?<t>\\w+)' was not present in the dictionary."
        },
        new[]
        {
            "SQLite Error 1: 'no such column: (?<t>\\w+)'.",
            "Column (?<t>\\w+) is not found"
        }
    };
    // Makes sure actual error either matches the expected completely,
    // or equivalent to any of it counterparts in the lookup table, using Regex
    protected static void AssertErrorsMatch(string expected, string actual)
    {
        if (expected == actual) return;
        var (equivalenceClass, expectedMatch) = ErrorEquivalenceTable
            .Select(errorSet => (errorSet, match: errorSet.FindMatch(expected)))
            .FirstOrDefault(t => t.match != null);
        if (equivalenceClass == null || expectedMatch == null)
        {
            Assert.Fail($"""
                         Expected error equivalence class is NOT found!
                         {expected}
                         """);
        }

        var actualMatch = equivalenceClass.FindMatch(actual);
        if (actualMatch == null)
        {
            Assert.Fail(
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