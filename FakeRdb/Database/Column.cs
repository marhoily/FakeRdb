namespace FakeRdb;

public sealed record Column(int ColumnIndex, string Name, 
    TypeAffinity ColumnType, bool IsAutoincrement = false);