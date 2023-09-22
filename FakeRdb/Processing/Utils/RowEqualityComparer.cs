namespace FakeRdb;

class RowEqualityComparer<T> : IEqualityComparer<List<T>>
{
    public bool Equals(List<T>? x, List<T>? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        return x.SequenceEqual(y);
    }

    public int GetHashCode(List<T> obj)
    {
        int hash = 17;
        foreach (var item in obj)
        {
            hash = hash * 31 + (item == null ? 0 : item.GetHashCode());
        }
        return hash;
    }
}