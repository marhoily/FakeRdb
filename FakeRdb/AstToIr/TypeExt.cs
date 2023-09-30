using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace FakeRdb;

public static partial class TypeExt
{
    public static bool? ToNullableBool(this object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            long l => l != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
    public static bool ToBool(this object value)
    {
        return value switch
        {
            bool b => b,
            long l => l != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
    [GeneratedRegex(@"^[-+]?((0(?![0-9])|[1-9]\d*)(\.\d*)?|\.\d+)([eE][-+]?\d+)?$")]
    private static partial Regex IsNumericRegex();

    public static bool IsNumeric(this string value)
    {
        return IsNumericRegex().IsMatch(value);
    }

    public static StorageType GetStorageType(
        this object? obj, TypeAffinity columnAffinity = TypeAffinity.None)
    {
        var dataAffinity = obj switch
        {
            null => StorageType.Null,
            string s => s.IsNumeric() ? s.IsInteger()
                ? StorageType.Integer
                : StorageType.Real
                : StorageType.Text,
            int or
                long or
                byte or
                char or
                byte => StorageType.Integer,
            decimal m => m.IsInteger()
                ? StorageType.Integer
                : StorageType.Real,
            double d => d.IsInteger()
                ? StorageType.Integer
                : StorageType.Real,
            float f => f.IsInteger()
                ? StorageType.Integer
                : StorageType.Real,
            _ => StorageType.Blob
        };
        return (dataAffinity, columnAffinity) switch
        {
            /* A column with TEXT affinity stores all data using
             * storage classes NULL, TEXT or BLOB. If numerical 
             * data is inserted into a column with TEXT affinity 
             * it is converted into text form before being stored.*/
            (StorageType.Null, TypeAffinity.Text)
                => StorageType.Null,
            (StorageType.Blob, TypeAffinity.Text)
                => StorageType.Blob,
            (_, TypeAffinity.Text)
                => StorageType.Text,

            /* A column with NUMERIC affinity may contain values
             * using all five storage classes. 
             *
             * A column that uses INTEGER affinity behaves the same
             * as a column with NUMERIC affinity.
             */
            (var n, TypeAffinity.Numeric or TypeAffinity.Integer)
                => n,

            (StorageType.Integer, TypeAffinity.Real)
                => StorageType.Real,
            (var r, TypeAffinity.Real)
                => r,
            /*
             * A column with affinity BLOB does not prefer one storage
             * class over another and no attempt is made to coerce
             * data from one storage class into another.
             */
            (_, TypeAffinity.Blob) => obj switch
            {
                null => StorageType.Null,
                string => StorageType.Text,
                int or
                    long or
                    byte or
                    char or
                    byte => StorageType.Integer,
                decimal => StorageType.Real,
                double => StorageType.Real,
                float => StorageType.Real,
                _ => StorageType.Blob
            },

            var (i, _) => i
        };
    }
    public static double CoerceToRealOrZero(this object? obj)
    {
        return obj switch
        {
            string s => double.TryParse(s, out var d) ? d : 0.0,
            long l => l,
            double d => d,
            _ => 0.0
        };
    }
    public static TypeAffinity GetTypeAffinity(this object? obj)
    {
        return obj switch
        {
            null => TypeAffinity.None,
            string => TypeAffinity.Text,
            int or
                bool or
                long or
                byte or
                char or
                byte => TypeAffinity.Integer,
            double or
                float => TypeAffinity.Real,
            _ => TypeAffinity.Blob
        };
    }
    public static TypeAffinity CalculateEffectiveAffinity(this object? obj)
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

    public static object? CoerceToStoredType(this object? obj)
    {
        return obj switch
        {
            null or long or string or double or byte[] => obj,
            bool b => b ? 1L : 0L,
            int i => (long)i,
            float f => (double)f,
            _ => throw new ArgumentOutOfRangeException(nameof(obj), obj.GetType().Name)
        };
    }
    public static object? Coerce(this object? obj, TypeAffinity affinity)
    {
        return (obj, obj.GetStorageType(affinity)) switch
        {
            (null, _) => null,
            (string s, StorageType.Integer) =>
                (long)BigInteger.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
            (_, StorageType.Integer) => Convert.ChangeType(obj, typeof(long)),
            (_, StorageType.Real) => Convert.ChangeType(obj, typeof(double)),
            (_, StorageType.Text) => Convert.ChangeType(obj, typeof(string)),
            (_, StorageType.Blob) => obj,
            _ => throw new ArgumentOutOfRangeException(nameof(affinity))
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
        var hex = input[2..^1];

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

        if (string.Equals(input, "true", StringComparison.OrdinalIgnoreCase))
        {
            return 1L;
        }

        if (string.Equals(input, "false", StringComparison.OrdinalIgnoreCase))
        {
            return 0L;
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