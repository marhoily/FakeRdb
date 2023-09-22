using Antlr4.Runtime;

namespace FakeRdb;

public static class FakeDbExt
{
    public static IResult? Execute(this Database db, string sql,
        FakeDbParameterCollection parameters)
    {
        var inputStream = new AntlrInputStream(sql);
        var lexer = new SQLiteLexer(inputStream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new SQLiteParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new PanicErrorListener());
        var chatContext = parser.sql_stmt_list();
        var visitor = new IrVisitor(sql, db,parameters);
        return visitor.Visit(chatContext);
    }
    public static DbDataReader ExecuteReader(this Database db, 
        string sql, FakeDbParameterCollection parameters)
    {
     
        return db.Execute(sql, parameters) switch
        {
            Affected affected => new RecordsAffectedDataReader(affected.RecordsCount),
            QueryResult queryResult => new FakeDbReader(queryResult),
            var x => throw new ArgumentOutOfRangeException(x?.ToString())
        };
    }
}