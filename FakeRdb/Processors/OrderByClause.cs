namespace FakeRdb;

public sealed class OrderByClause : IResult
{
    private readonly Field _field;

    public OrderByClause(Field field) => _field = field;

    public IComparer<List<object?>> GetComparer(Field[] schema) => 
        new Comparer(Array.IndexOf(schema, _field));

    private sealed class Comparer : IComparer<List<object?>>
    {
        private readonly int _columnIndex;

        public Comparer(int columnIndex) => _columnIndex = columnIndex;

        public int Compare(List<object?>? x, List<object?>? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, x)) return 1;
            if (ReferenceEquals(null, y)) return -1;

            var a = x[_columnIndex];
            var b = y[_columnIndex];
            if (ReferenceEquals(a, b)) return 0;
            if (ReferenceEquals(null, a)) return 1;
            if (ReferenceEquals(null, b)) return -1;
            if (a is IComparable ca) return ca.CompareTo(b);
            if (b is IComparable cb) return -cb.CompareTo(a);
            throw new NotImplementedException();
        }
    }
}