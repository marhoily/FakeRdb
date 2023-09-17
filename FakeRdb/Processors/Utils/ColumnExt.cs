namespace FakeRdb;

public static class ColumnExt
{
    public static string GetColumnName(this SQLiteParser.Result_columnContext context)
    {
        var r = context.GetText();
        return r[0] is '[' or '`' or '"' 
            ? r[1..^1] 
            : r;
    }
}