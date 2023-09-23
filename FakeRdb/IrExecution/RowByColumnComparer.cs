namespace FakeRdb;

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

    public int Compare(Row? x, Row? y)
    {
        return CompareList(x?.Data, y?.Data);
    }

    
    public int Compare(object?[]? x, object?[]? y)
    {
        return CompareList(x, y);
    }
    public int Compare(List<object?>? x, List<object?>? y)
    {
        return CompareList(x, y);
    }

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