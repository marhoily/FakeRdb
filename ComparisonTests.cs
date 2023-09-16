using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace FakeRdb;

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

    protected void CompareReaders(string sql)
    {
        var cmd1 = Prototype.CreateCommand();
        cmd1.CommandText = sql;
        var reader = cmd1.ExecuteReader();

        var cmd2 = Sut.CreateCommand();
        cmd2.CommandText = sql;
        var result = cmd2.ExecuteReader();

        reader.ShouldEqual(result);
    }
}