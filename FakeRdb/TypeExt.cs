namespace FakeRdb;

public static class TypeExt
{
    public static Type ToRuntimeType(this SQLiteParser.Type_nameContext context)
    {
        return context.GetText() switch
        {
            "TEXT" => typeof(string),
            "INTEGER" => typeof(long),
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}