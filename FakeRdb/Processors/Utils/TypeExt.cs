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
        return context.GetText() switch
        {
            "TEXT" => DynamicType.Text,
            "INTEGER" => DynamicType.Integer,
            "NUMERIC" => DynamicType.Numeric,
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}