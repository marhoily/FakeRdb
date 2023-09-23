namespace FakeRdb;

public sealed class ObjectComparer : IComparer<object?>
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