using Microsoft.Data.Sqlite;

namespace FakeRdb.Tests;

public abstract class ComparisonTestBase : IDisposable
{
    private readonly ITestOutputHelper _output;
    protected DbConnection Sqlite { get; } = new SqliteConnection("Data Source=:memory:");
    protected DbConnection Sut { get; } = new FakeDbConnection(new Database());

    protected ComparisonTestBase(ITestOutputHelper output)
    {
        _output = output;
        Sqlite.Open();
        Sut.Open();
    }

    public void Dispose()
    {
        Sqlite.Dispose();
        Sut.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void ExecuteOnBoth(string sql)
    {
        var cmd1 = Sqlite.CreateCommand();
        cmd1.CommandText = sql;
        cmd1.SafeExecuteReader();

        var cmd2 = Sut.CreateCommand();
        cmd2.CommandText = sql;
        cmd2.SafeExecuteReader();
    }

    protected void CompareAgainstSqlite(string sql, string? description = null, bool printOut = true)
    {
        if (printOut)
        {
            if (description != null) _output.WriteLine($"--- {description} ---");
            _output.WriteLine(sql);
        }

        var cmd1 = Sqlite.CreateCommand();
        cmd1.CommandText = sql;
        var (expected, x1) = cmd1.SafeExecuteReader();

        var cmd2 = Sut.CreateCommand();
        cmd2.CommandText = sql;
        var (actual, x2) = cmd2.SafeExecuteReader();
        if (x1 != null)
        {
            x2.Should().NotBeNull();
            _output.WriteLine(x1.Message);
            ErrorEquivalence.Assert(x1.Message, x2!.Message);
        }
        else if (x2 != null)
        {
            Assert.Fail("While prototype DB ran without errors, " +
                        "FakeDb gave this one:\n\n" + x2);
        }
        else
        {
            actual!.ShouldEqual(expected!, _output, printOut);
        }
    }
}