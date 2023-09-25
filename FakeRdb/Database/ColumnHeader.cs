namespace FakeRdb;

public sealed record ColumnHeader(int ColumnIndex, string Name, 
    TypeAffinity ColumnType, bool IsAutoincrement = false);

public sealed class Column : List<object?>
{
    public Column(ColumnHeader header)
    {
        Header = header;
    }

    public ColumnHeader Header { get; }
}
