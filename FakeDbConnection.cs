using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace FakeRdb;

public sealed class FakeDbConnection : DbConnection
{
    private ConnectionState _state;

    public FakeDbConnection(FakeDb db)
    {
        Db = db;
        _state = ConnectionState.Closed;
    }

    [AllowNull]
    public override string ConnectionString
    {
        get => "";
        set
        {
            
        }
    }

    public override string Database => "";
    public override string DataSource => "";
    public override string ServerVersion=> "";
    public override ConnectionState State => _state;

    public FakeDb Db { get; set; }

    protected override DbCommand CreateDbCommand()
    {
        return CreateCommand();
    }

    public new FakeDbCommand CreateCommand()
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