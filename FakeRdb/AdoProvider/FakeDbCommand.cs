using System.Diagnostics.CodeAnalysis;

namespace FakeRdb;

public class FakeDbCommand : DbCommand
{
    private readonly FakeDbConnection _connection;

    public FakeDbCommand(FakeDbConnection connection)
    {
        _connection = connection;
        Parameters = new FakeDbParameterCollection();
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
    protected override DbParameterCollection DbParameterCollection => Parameters;
    protected override DbTransaction? DbTransaction { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public new FakeDbParameterCollection Parameters { get; }
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        return behavior switch
        {
            CommandBehavior.Default => _connection.Db.ExecuteReader(CommandText, Parameters),
            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior, null)
        };
    }

    public override int ExecuteNonQuery()
    {
        var affected = (Affected?)_connection.Db
            .Execute(CommandText, Parameters);
        return affected?.RecordsCount ?? 0;
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
