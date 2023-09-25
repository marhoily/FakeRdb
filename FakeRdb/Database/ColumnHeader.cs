namespace FakeRdb;

public sealed record ColumnHeader(int ColumnIndex, string Name, 
    TypeAffinity ColumnType, bool IsAutoincrement = false);