using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;

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
        throw new NotImplementedException();
    }

    public override int ExecuteNonQuery()
    {
        var inputStream = new AntlrInputStream(CommandText);
        var lexer = new SQLiteLexer(inputStream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new SQLiteParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new PanicErrorListener());
        var chatContext = parser.sql_stmt_list();
        var visitor = new NonQueryVisitor(_connection.Db, Parameters);
        return visitor.Visit(chatContext);

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
