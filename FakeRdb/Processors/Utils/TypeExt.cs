using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace FakeRdb;

public static partial class TypeExt
{
    // Numeric strings consist of optional sign,
    // any number of digits, optional decimal part and optional
    // exponential part. Thus +0123.45e6 is a valid numeric value.
    [GeneratedRegex(@"^(((?!0)|[-+]|(?=0+\.))(\d*\.)?\d+(e\d+)?)$")]
    private static partial Regex IsNumericRegex();

    static bool IsNumeric(this string value)
    {
        return IsNumericRegex().IsMatch(value);
    }
    public static SqliteStorageType GetStorageTypeType(
        this object? obj, SqliteTypeAffinity columnAffinity)
    {
        var dataAffinity = obj switch
        {
            null => SqliteStorageType.Null,
            string s => s.IsNumeric() ? s.IsInteger()
                ? SqliteStorageType.Integer
                : SqliteStorageType.Real
                : SqliteStorageType.Text,
            int or 
                long or 
                byte or 
                char or 
                byte => SqliteStorageType.Integer,
            decimal m => m.IsInteger() 
                ? SqliteStorageType.Integer 
                : SqliteStorageType.Real,
            double d => d.IsInteger() 
                ? SqliteStorageType.Integer 
                : SqliteStorageType.Real,
            float f => f.IsInteger() 
                ? SqliteStorageType.Integer 
                : SqliteStorageType.Real,
            _ => throw new ArgumentOutOfRangeException(obj.GetType().Name)
        };
        return (dataAffinity, columnAffinity) switch
        {
            /* A column with TEXT affinity stores all data using
             * storage classes NULL, TEXT or BLOB. If numerical 
             * data is inserted into a column with TEXT affinity 
             * it is converted into text form before being stored.*/
            (SqliteStorageType.Null, SqliteTypeAffinity.Text)
                => SqliteStorageType.Null,
            (SqliteStorageType.Blob, SqliteTypeAffinity.Text)
                => SqliteStorageType.Blob,
            (_, SqliteTypeAffinity.Text)
                => SqliteStorageType.Text,

            /* A column with NUMERIC affinity may contain values
             * using all five storage classes. 
             *
             * A column that uses INTEGER affinity behaves the same
             * as a column with NUMERIC affinity.
             */
            (var n, SqliteTypeAffinity.Numeric or SqliteTypeAffinity.Integer)
                => n,

            (SqliteStorageType.Integer, SqliteTypeAffinity.Real)
                => SqliteStorageType.Real,
            (var r, SqliteTypeAffinity.Real)
                => r,
            /*
             * A column with affinity BLOB does not prefer one storage
             * class over another and no attempt is made to coerce
             * data from one storage class into another.
             */
            var (i, _) => i
        };
    }

    public static bool IsInteger(this string text)
    {
        return BigInteger.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }
    public static bool IsInteger(this double value)
    {
        return Math.Abs((long)value - value) <= double.Epsilon;
    }
    public static bool IsInteger(this float value)
    {
        return Math.Abs((long)value - value) <= double.Epsilon;
    }
    public static bool IsInteger(this decimal value)
    {
        return value % 1 == 0;
    }
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