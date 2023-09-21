namespace FakeRdb;

public sealed class OrderByClause : IResult
{
    private readonly Field _field;

    public OrderByClause(Field field) => _field = field;

    public IComparer<List<object?>> GetComparer(ResultSchema schema) =>
        new Comparer(schema.IndexOf(_field));

    private sealed class Comparer : IComparer<List<object?>>
    {
        private readonly int _columnIndex;

        public Comparer(int columnIndex)
        {
            if (columnIndex < 0) throw new ArgumentOutOfRangeException(nameof(columnIndex));
            _columnIndex = columnIndex;
        }

        public int Compare(List<object?>? x, List<object?>? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return 1;
            if (y == null) return -1;

            var a = x[_columnIndex];
            var b = y[_columnIndex];
            if (ReferenceEquals(a, b)) return 0;
            return ComparisonRules.CompareSqliteObjects(a, b);
        }

    }
}