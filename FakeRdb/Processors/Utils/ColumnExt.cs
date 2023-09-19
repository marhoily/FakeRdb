namespace FakeRdb;

public static class ColumnExt
{
    public static string Unescape(this string r)
    {
        return r[0] is '[' or '`' or '"' ? r[1..^1] : r;
    }
    public static string Unquote(this string r)
    {
        return r[0] is '\'' ? r[1..^1] : r;
    }
}