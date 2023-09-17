namespace FakeRdb;

public static class ColumnExt
{
    public static string GetColumnName(this SQLiteParser.Result_columnContext context)
    {
        return context.GetText().Unescape();
    }

    public static string Unescape(this string r)
    {
        return r[0] is '[' or '`' or '"' ? r[1..^1] : r;
    }
}