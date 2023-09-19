namespace FakeRdb;

public static class TypeExt
{
    public static DynamicType GetRuntimeType(this object? obj)
    {
        if (obj == null) return DynamicType.Null;
        return obj switch
        {
            string => DynamicType.Text,
            int => DynamicType.Integer,
            long => DynamicType.Integer,
            decimal => DynamicType.Numeric,
            _ => throw new ArgumentOutOfRangeException(obj.GetType().Name)
        };
    }
    public static DynamicType ToRuntimeType(this SQLiteParser.Type_nameContext context)
    {
        return context.GetText() switch
        {
            "TEXT" => DynamicType.Text,
            "INTEGER" => DynamicType.Integer,
            "NUMERIC" => DynamicType.Numeric,
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}