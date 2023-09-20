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
        /* https://www.sqlite.org/datatype3.html
         * 3.1. Determination Of Column Affinity
         *
         * For tables not declared as STRICT, the affinity of a column is determined by the
         * declared type of the column, according to the following rules in the order shown:
         *
         * If the declared type contains the string "INT" then it is assigned INTEGER affinity.
         *
         * If the declared type of the column contains any of the strings "CHAR", "CLOB", or
         * "TEXT" then that column has TEXT affinity. Notice that the type VARCHAR contains
         * the string "CHAR" and is thus assigned TEXT affinity.
         *
         * If the declared type for a column contains the string "BLOB" or if no type is
         * specified then the column has affinity BLOB.
         *
         * If the declared type for a column contains any of the strings "REAL", "FLOA",
         * or "DOUB" then the column has REAL affinity.
         *
         * Otherwise, the affinity is NUMERIC.
         *
         * Note that the order of the rules for determining column affinity is important.
         * A column whose declared type is "CHARINT" will match both rules 1 and 2 but the
         * first rule takes precedence and so the column affinity will be INTEGER.
         *
         */
        return context.GetText() switch
        {
            "TEXT" => DynamicType.Text,
            "INTEGER" => DynamicType.Integer,
            "NUMERIC" => DynamicType.Numeric,
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}