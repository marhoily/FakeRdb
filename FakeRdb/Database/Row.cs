namespace FakeRdb;

public sealed record Row(params object?[] Data)
{
    public override string ToString() => string.Join(", ", Data);

    public object? this[ColumnHeader column] => Data[column.ColumnIndex];

    public static RowByColumnComparer Comparer(int columnIndex) => new(columnIndex);
    public static readonly RowEqualityComparer<object?> EqualityComparer = new();
    public Row Concat(Row second)
    {
        return new Row(Data.Concat(second.Data).ToArray());
    }
    
    /// <summary>
    /// Generates a CompositeKey based on the values of the specified columns.
    /// </summary>
    /// <param name="columns">An IEnumerable{int} representing the indices of the columns to include in the CompositeKey.</param>
    /// <returns>A CompositeKey object.</returns>
    public CompositeKey GetKey(IEnumerable<int> columns)
    {
        var keyComponents = new List<object?>();
        foreach (var columnIndex in columns)
        {
            if (columnIndex < 0 || columnIndex >= Data.Length)
            {
                throw new IndexOutOfRangeException($"Column index {columnIndex} is out of range.");
            }

            keyComponents.Add(Data[columnIndex]);
        }

        return new CompositeKey(keyComponents.ToArray());
    }

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
            return CustomFieldComparer.Compare(a, b);
        }
    }

    public class RowEqualityComparer<T> : 
        IEqualityComparer<List<T>>,
        IEqualityComparer<Row>
    {
        public bool Equals(Row? x, Row? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);
            return x.Data.SequenceEqual(y.Data);
        }

        public bool Equals(List<T>? x, List<T>? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);
            return x.SequenceEqual(y);
        }

        public int GetHashCode(Row obj) 
        {
            int hash = 17;
            foreach (var item in obj.Data)
            {
                hash = hash * 31 + (item == null ? 0 : item.GetHashCode());
            }
            return hash;
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

    /// <summary>
    /// Represents a composite key based on the values from one or more columns.
    /// </summary>
    public sealed class CompositeKey : IEquatable<CompositeKey>, IComparable<CompositeKey> 
    {
        private readonly object?[] _keyComponents;

        public CompositeKey(params object?[] keyComponents)
        {
            _keyComponents = keyComponents ?? throw new ArgumentNullException(nameof(keyComponents));
        }

        public int CompareTo(CompositeKey? other)
        {
            if (other == null) return 0;
            return CustomFieldComparer.Compare(
                _keyComponents[0], other._keyComponents[0]);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as CompositeKey);
        }

        public bool Equals(CompositeKey? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return _keyComponents.SequenceEqual(other._keyComponents);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var component in _keyComponents) 
                hash.Add(component);
            return hash.ToHashCode();
        }

        public override string ToString() => string.Join(", ", _keyComponents);
    }

}