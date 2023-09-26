namespace FakeRdb;

/// <summary>
/// 
/// </summary>
/// <param name="ColumnIndex"></param>
/// <param name="Name"></param>
/// <param name="FullName">for calculated columns and
///     columns with alias Name and FullName are the same</param>
/// <param name="ColumnType"></param>
/// <param name="IsAutoincrement"></param>
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
