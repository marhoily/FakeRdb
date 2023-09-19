using Antlr4.Runtime.Tree;

namespace FakeRdb;

public static class ExpExt
{
    public static Func<Row, bool> ToFilter(this SQLiteParser.ExprContext ctx, Table table)
    {
        if (ctx.children.Count != 3) throw new NotImplementedException();
        if (ctx.children[1] is not ITerminalNode { Symbol.Type: SQLiteLexer.ASSIGN })
            throw new NotImplementedException();
        if (ctx.expr().Length != 2)
            throw new NotImplementedException();
        if (ctx.expr(0).column_access() is not { } column)
            throw new NotImplementedException();
        if (ctx.expr(1).literal_value() is not { } literal)
            throw new NotImplementedException();
        var col = table.Schema.IndexOf(column.GetText());
        var val = Convert.ChangeType(literal.GetText(), 
            table.Schema.Columns[col].FieldType);
        return row => Equals(row.Data[col], val);
    }
    public static object? Resolve(this SQLiteParser.ExprContext ctx, 
        FakeDbParameterCollection parameters)
    {
        if (ctx.BIND_PARAMETER() is { } bind)
        {
            return parameters[bind.GetText()].Value;
        }
        if (ctx.literal_value() is {} literal)
        {
            return literal.GetText().Unquote();
        }

        throw new NotImplementedException(ctx.GetText());
    }
}