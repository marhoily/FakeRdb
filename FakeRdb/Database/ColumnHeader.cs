namespace FakeRdb;

public sealed record ColumnHeader(int ColumnIndex, string Name,
    TypeAffinity ColumnType, bool IsAutoincrement = false)
{
    public ColumnDefinition ToDefinition()
    {
        return new ColumnDefinition(Name, ColumnType);
    }
}

public sealed record Column(ColumnHeader Header, List<object?> Rows)
{
    public Column Derive(string? columnAlias)
    {
        if (columnAlias == null) return this;
        return this with { Header = Header with { Name = columnAlias } };
    }
}
