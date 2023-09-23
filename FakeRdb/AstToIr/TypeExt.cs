using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace FakeRdb;

public static partial class TypeExt
{
    public static readonly IComparer<object?> Comparer = new ObjectComparer();


    [GeneratedRegex(@"^[-+]?((0(?![0-9])|[1-9]\d*)(\.\d*)?|\.\d+)([eE][-+]?\d+)?$")]
    private static partial Regex IsNumericRegex();

    public static bool IsNumeric(this string value)
    {
        return IsNumericRegex().IsMatch(value);
    }

    public static SqliteStorageType GetStorageType(
        this object? obj, TypeAffinity columnAffinity = TypeAffinity.None)
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
            (SqliteStorageType.Null, TypeAffinity.Text)
                => SqliteStorageType.Null,
            (SqliteStorageType.Blob, TypeAffinity.Text)
                => SqliteStorageType.Blob,
            (_, TypeAffinity.Text)
                => SqliteStorageType.Text,

            /* A column with NUMERIC affinity may contain values
             * using all five storage classes. 
             *
             * A column that uses INTEGER affinity behaves the same
             * as a column with NUMERIC affinity.
             */
            (var n, TypeAffinity.Numeric or TypeAffinity.Integer)
                => n,

            (SqliteStorageType.Integer, TypeAffinity.Real)
                => SqliteStorageType.Real,
            (var r, TypeAffinity.Real)
                => r,
            /*
             * A column with affinity BLOB does not prefer one storage
             * class over another and no attempt is made to coerce
             * data from one storage class into another.
             */
            (_, TypeAffinity.Blob) => obj switch
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
    public static TypeAffinity GetTypeAffinity(this object? obj)
    {
        return obj switch
        {
            null => TypeAffinity.None,
            string => TypeAffinity.Text,
            int or
                long or
                byte or
                char or
                byte => TypeAffinity.Integer,
            double or 
                float => TypeAffinity.Real,
            _ => TypeAffinity.Blob
        };
    }
    public static TypeAffinity GetSimplifyingAffinity(this object? obj)
    {
        return obj switch
        {
            null => TypeAffinity.None,
            string s => s.IsNumeric() ? s.IsInteger()
                ? TypeAffinity.Integer
                : TypeAffinity.Real
                : TypeAffinity.Text,
            int or
                long or
                byte or
                char or
                byte => TypeAffinity.Integer,
            decimal m => m.IsInteger()
                ? TypeAffinity.Integer
                : TypeAffinity.Real,
            double d => d.IsInteger()
                ? TypeAffinity.Integer
                : TypeAffinity.Real,
            float f => f.IsInteger()
                ? TypeAffinity.Integer
                : TypeAffinity.Real,
            _ => TypeAffinity.Blob
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

    public static object? Coerce(this object? obj, TypeAffinity affinity)
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
    public static object? ConvertToSqliteType(this object? input, TypeAffinity affinity)
    {
        if (input is not string s)
        {
            return input;
        }

        var result = affinity switch
        {
            TypeAffinity.Integer =>
                long.TryParse(s, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var integer) ? integer : input,
            TypeAffinity.Real =>
                double.TryParse(s, out var real) ? real : input,
            TypeAffinity.Numeric =>
                long.TryParse(s, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var integer) ? integer :
                double.TryParse(s, out var real) ? real : input,
            _ => input
        };
        return result;

    }

    private static byte[] ConvertHexToBytes(this string input)
    {
        // Extract the hexadecimal part
        var hex = input.Substring(2, input.Length - 3);

        // Convert the hexadecimal string to a byte array
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
        {
            var byteValue = hex.Substring(i, 2);
            bytes[i / 2] = Convert.ToByte(byteValue, 16);
        }

        return bytes;
    }

    [GeneratedRegex("^x'[0-9a-fA-F]*'$")]
    private static partial Regex IsBlob();


    public static object? CoerceToLexicalAffinity(this string? input)
    {
        if (input == null)
        {
            return null;
        }

        if (string.Equals(input, "NULL", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (IsBlob().IsMatch(input))
        {
            return input.ConvertHexToBytes();
        }

        if (long.TryParse(input, out var integer))
        {
            return integer;
        }

        if (double.TryParse(input, out var real))
        {
            return real;
        }

        if (input.StartsWith("'") && input.EndsWith("'"))
        {
            return input[1..^1];
        }

        return input;
    }
    public static TypeAffinity GetLexicalAffinity(this string? input)
    {
        if (input == null)
        {
            return TypeAffinity.None;
        }

        if (string.Equals(input, "NULL", StringComparison.OrdinalIgnoreCase))
        {
            return TypeAffinity.None;
        }

        if (IsBlob().IsMatch(input))
        {
            return TypeAffinity.Blob;
        }

        if (long.TryParse(input, out _))
        {
            return TypeAffinity.Integer;
        }

        if (double.TryParse(input, out _))
        {
            return TypeAffinity.Real;
        }

        if (input.StartsWith("'") && input.EndsWith("'"))
        {
            return TypeAffinity.Text;
        }

        return TypeAffinity.None;
    }
    public static TypeAffinity ToRuntimeType(this SQLiteParser.Type_nameContext? context)
    {
        return context?.GetText().ToUpperInvariant() switch
        {
            null => TypeAffinity.NotSet,
            "TEXT" => TypeAffinity.Text,
            "INTEGER" => TypeAffinity.Integer,
            "NUMERIC" => TypeAffinity.Numeric,
            "REAL" => TypeAffinity.Real,
            "BLOB" => TypeAffinity.Blob,
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}