using Microsoft.Data.Sqlite;

namespace FakeRdb.Tests;

public abstract class ComparisonTestBase : IDisposable
{
    protected DbConnection SqliteConnection { get; } = new SqliteConnection("Data Source=:memory:");
    protected DbConnection SutConnection { get; } = new FakeDbConnection(new Database());

    protected ComparisonTestBase()
    {
        SqliteConnection.Open();
        SutConnection.Open();
    }

    public void Dispose()
    {
        SqliteConnection.Dispose();
        SutConnection.Dispose();
        GC.SuppressFinalize(this);
    }

}