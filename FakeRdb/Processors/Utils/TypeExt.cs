using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace FakeRdb;

public static partial class TypeExt
{
    public static readonly IComparer<object?> Comparer = new SqliteComparer();
    private sealed class SqliteComparer : IComparer<object?>
    {
        public int Compare(object? x, object? y)
        {
            return (x, y) switch
            {
                (null, null) => 0,
                (null, _) => -1,
                (_, null) => -1,
                (long a, double b) => ((double)a).CompareTo(b),
                (IComparable a, _) => a.CompareTo(y),
                //(long a, long b) => a.CompareTo(b),
                _ => throw new NotImplementedException(
                    $"Comparer of: {x.GetType().Name}; {y.GetType().Name}")
            };
        }
    }
    [GeneratedRegex(@"^[-+]?((0(?![0-9])|[1-9]\d*)(\.\d*)?|\.\d+)([eE][-+]?\d+)?$")]
    private static partial Regex IsNumericRegex();

    public static bool IsNumeric(this string value)
    {
        return IsNumericRegex().IsMatch(value);
    }

    public static SqliteStorageType GetStorageType(
        this object? obj, SqliteTypeAffinity columnAffinity = SqliteTypeAffinity.None)
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
            _ => SqliteStorageType.Blob
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
            (_, SqliteTypeAffinity.Blob) => obj switch
            {
                null => SqliteStorageType.Null,
                string => SqliteStorageType.Text,
                int or
                    long or
                    byte or
                    char or
                    byte => SqliteStorageType.Integer,
                decimal => SqliteStorageType.Real,
                double => SqliteStorageType.Real,
                float => SqliteStorageType.Real,
                _ => SqliteStorageType.Blob
            },

            var (i, _) => i
        };
    }
    public static SqliteTypeAffinity GetTypeAffinity(this object? obj)
    {
        return obj switch
        {
            null => SqliteTypeAffinity.None,
            string s => s.IsNumeric() ? s.IsInteger()
                ? SqliteTypeAffinity.Integer
                : SqliteTypeAffinity.Real
                : SqliteTypeAffinity.Text,
            int or
                long or
                byte or
                char or
                byte => SqliteTypeAffinity.Integer,
            decimal m => m.IsInteger()
                ? SqliteTypeAffinity.Integer
                : SqliteTypeAffinity.Real,
            double d => d.IsInteger()
                ? SqliteTypeAffinity.Integer
                : SqliteTypeAffinity.Real,
            float f => f.IsInteger()
                ? SqliteTypeAffinity.Integer
                : SqliteTypeAffinity.Real,
            _ => SqliteTypeAffinity.Blob
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

    public static object? Coerce(this object? obj, SqliteTypeAffinity affinity)
    {
        return (obj, obj.GetStorageType(affinity)) switch
        {
            (null, _) => null,
            (string s, SqliteStorageType.Integer) =>
                (long)BigInteger.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
            (_, SqliteStorageType.Integer) => Convert.ChangeType(obj, typeof(long)),
            (_, SqliteStorageType.Real) => Convert.ChangeType(obj, typeof(double)),
            (_, SqliteStorageType.Text) => Convert.ChangeType(obj, typeof(string)),
            (_, SqliteStorageType.Blob) => obj,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static SqliteTypeAffinity ToRuntimeType(this SQLiteParser.Type_nameContext? context)
    {
        return context?.GetText() switch
        {
            null => SqliteTypeAffinity.None,
            "TEXT" => SqliteTypeAffinity.Text,
            "INTEGER" => SqliteTypeAffinity.Integer,
            "NUMERIC" => SqliteTypeAffinity.Numeric,
            "REAL" => SqliteTypeAffinity.Real,
            "BLOB" => SqliteTypeAffinity.Blob,
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}