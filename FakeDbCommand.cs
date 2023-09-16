using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace FakeRdb;

public class FakeDbCommand : DbCommand
{
    private readonly FakeDbConnection _connection;

    public FakeDbCommand(FakeDbConnection connection)
    {
        _connection = connection;
        DbParameterCollection = new FakeDbParameterCollection();
    }

    public override void Prepare()
    {
        throw new NotImplementedException();
    }

    [AllowNull]
    public override string CommandText { get; set; }
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
        if (_connection.State != ConnectionState.Open)
            throw new InvalidOperationException("The connection must be open to execute a command.");

        // Execute the scalar query and return the result
        Console.WriteLine("Executing query: " + CommandText);
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
}