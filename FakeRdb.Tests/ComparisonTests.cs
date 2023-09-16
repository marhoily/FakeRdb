using System.Data.Common;
using System.Text.RegularExpressions;
using FluentAssertions;
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
        }
    };
    // Makes sure actual error either matches the expected completely,
    // or equivalent to any of it counterparts in the lookup table, using Regex
    private static void AssertErrorsMatch(string expected, string actual)
    {
        if (expected == actual) return;
        var equivalenceClass = ErrorEquivalenceTable.FirstOrDefault(
            errorSet => errorSet.Any(errorPattern => Regex.IsMatch(expected, errorPattern)));
        equivalenceClass.Should().NotBeNull($"Expected error is NOT found!\n{expected}");
        equivalenceClass.Should().Contain(errorPattern => Regex.IsMatch(actual, errorPattern));
    }
}