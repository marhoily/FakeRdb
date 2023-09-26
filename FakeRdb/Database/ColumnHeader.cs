namespace FakeRdb;

public sealed record ColumnHeader(int ColumnIndex, string Name,
    string FullName,
    TypeAffinity ColumnType, bool IsAutoincrement = false);

public sealed record Column(ColumnHeader Header, List<object?> Rows)
{
    public Column Derive(string? columnAlias)
    {
        if (columnAlias == null) return this;
        return this with { Header = Header with { Name = columnAlias } };
    }
}
