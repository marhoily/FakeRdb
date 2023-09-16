using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace FakeRdb;

public sealed class FakeDbConnection : DbConnection
{
    private string _connectionString;
    private ConnectionState _state;

    public FakeDbConnection(string connectionString)
    {
        _connectionString = connectionString;
        _state = ConnectionState.Closed;
    }

    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (_state != ConnectionState.Closed)
                throw new InvalidOperationException("The connection state must be closed to set the connection string.");
            _connectionString = value ?? "";
        }
    }

    public override string Database => "";
    public override string DataSource => "";
    public override string ServerVersion=> "";
    public override ConnectionState State => _state;

    protected override DbCommand CreateDbCommand()
    {
        return new FakeDbCommand(this);
    }

    public override void Open()
    {
        if (_state == ConnectionState.Open)
            throw new InvalidOperationException("The connection is already open.");
        _state = ConnectionState.Open;
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw new NotImplementedException();
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("Changing the database is not supported in this toy SQLite provider.");
    }
}