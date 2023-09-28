namespace FakeRdb;

public sealed class CustomFieldComparer : IComparer<object?>
{
    int IComparer<object?>.Compare(object? x, object? y)
    {
        return Compare(x, y);
    }

    public new static bool Equals(object? a, object? b)
        => Compare(a, b) == 0;
    public static int Compare(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        // When the types are the same
        if (a.GetType() == b.GetType())
        {
            if (a is long longA)
            {
                long longB = (long)b;
                return longA.CompareTo(longB);
            }

            if (a is double doubleA)
            {
                double doubleB = (double)b;
                return doubleA.CompareTo(doubleB);
            }

            if (a is string stringA)
            {
                string stringB = (string)b;
                return string.Compare(stringA, stringB, StringComparison.Ordinal);
            }

            if (a is byte[] blobA)
            {
                byte[] blobB = (byte[])b;
                for (int i = 0; i < Math.Min(blobA.Length, blobB.Length); ++i)
                {
                    int comparison = blobA[i].CompareTo(blobB[i]);
                    if (comparison != 0)
                    {
                        return comparison;
                    }
                }
                return blobA.Length.CompareTo(blobB.Length);
            }
        }
        // When the types are different
        else
        {
            var aIsNumeric = a.GetTypeAffinity() is TypeAffinity.Real or TypeAffinity.Integer;
            var bIsNumeric = b.GetTypeAffinity() is TypeAffinity.Real or TypeAffinity.Integer;
            if (aIsNumeric && bIsNumeric)
            {
                if (a is double aa)
                    return aa.CompareTo(Convert.ChangeType(b, typeof(double)));
                if (b is double bb)
                    return -bb.CompareTo(Convert.ChangeType(a, typeof(double)));

            }
            if (a is long || b is long)
                return a is long ? -1 : 1;

            if (a is double || b is double)
                return a is double ? -1 : 1;

            if (a is string || b is string)
                return a is string ? -1 : 1;

            if (a is byte[] || b is byte[])
                return a is byte[]? -1 : 1;
        }

        throw new ArgumentException("Incompatible types");
    }
}