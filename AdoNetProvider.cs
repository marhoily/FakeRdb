using System;
using System.Data;
using System.Data.Common;

namespace FakeRdb;

public sealed class ToySqliteConnection : DbConnection
{
    private string _connectionString;
    private ConnectionState _state;

    public ToySqliteConnection(string connectionString)
    {
        _connectionString = connectionString;
        _state = ConnectionState.Closed;
    }

    public override string ConnectionString
    {
        get { return _connectionString; }
        set
        {
            if (_state != ConnectionState.Closed)
                throw new InvalidOperationException("The connection state must be closed to set the connection string.");
            _connectionString = value;
        }
    }

    public override string Database { get; }
    public override string DataSource { get; }
    public override string ServerVersion { get; }
    public override ConnectionState State => _state;

    protected override DbCommand CreateDbCommand()
    {
        return new ToySqliteCommand(this);
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

public class ToySqliteCommand : DbCommand
{
    private ToySqliteConnection connection;
    private string commandText;

    public ToySqliteCommand(ToySqliteConnection connection)
    {
        this.connection = connection;
    }

    public override void Prepare()
    {
        throw new NotImplementedException();
    }

    public override string CommandText
    {
        get { return commandText; }
        set { commandText = value; }
    }

    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection { get; }
    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get; set; }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        throw new NotImplementedException();
    }

    public override int ExecuteNonQuery()
    {
        throw new NotSupportedException("ExecuteNonQuery is not supported in this toy SQLite provider.");
    }

    public override object ExecuteScalar()
    {
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("The connection must be open to execute a command.");
        
        // Execute the scalar query and return the result
        Console.WriteLine("Executing query: " + commandText);
        return "ToySQLiteResult";
    }

    public override void Cancel()
    {
        throw new NotImplementedException();
    }

    protected override DbParameter CreateDbParameter()
    {
        throw new NotSupportedException("Creating parameters is not supported in this toy SQLite provider.");
    }

    // Other methods and properties omitted for brevity...
}