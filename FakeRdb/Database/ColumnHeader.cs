namespace FakeRdb;

public sealed record ColumnHeader(int ColumnIndex, string Name, 
    TypeAffinity ColumnType, bool IsAutoincrement = false);

public sealed record Column(ColumnHeader Header, List<object?> Rows);
