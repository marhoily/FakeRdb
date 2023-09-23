namespace FakeRdb;

public sealed record Row(object?[] Data)
{
    public object? this[Column column] => Data[column.ColumnIndex];

    public static RowByColumnComparer Comparer(int columnIndex) => new(columnIndex);
    public static readonly RowEqualityComparer<object?> EqualityComparer = new();

    public sealed class RowByColumnComparer : 
        IComparer<List<object?>>, 
        IComparer<object?[]>, 
        IComparer<Row>
    {
        private readonly int _columnIndex;

        public RowByColumnComparer(int columnIndex)
        {
            if (columnIndex < 0) throw new ArgumentOutOfRangeException(nameof(columnIndex));
            _columnIndex = columnIndex;
        }

        public int Compare(Row? x, Row? y) => CompareList(x?.Data, y?.Data);
        public int Compare(object?[]? x, object?[]? y) => CompareList(x, y);
        public int Compare(List<object?>? x, List<object?>? y) => CompareList(x, y);

        private int CompareList(IList<object?>? x, IList<object?>? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            var a = x[_columnIndex];
            var b = y[_columnIndex];
            if (ReferenceEquals(a, b)) return 0;
            return ObjectComparer.Compare(a, b);
        }
    }

    public class RowEqualityComparer<T> : IEqualityComparer<List<T>>
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
}