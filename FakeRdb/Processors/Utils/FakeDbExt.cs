using Antlr4.Runtime;

namespace FakeRdb;

public static class FakeDbExt
{
    public static DbDataReader ExecuteReader(this FakeDb db, string sql,
        FakeDbParameterCollection parameters)
    {
        var inputStream = new AntlrInputStream(sql);
        var lexer = new SQLiteLexer(inputStream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new SQLiteParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new PanicErrorListener());
        var chatContext = parser.sql_stmt_list();
        var visitor = new SqlVisitor(db,parameters);
        return visitor.Visit(chatContext) ?? new RecordsAffectedDataReader(0);
    }
}