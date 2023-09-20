namespace FakeRdb;

public sealed record Field(int ColumnIndex, string Name, 
    SqliteTypeAffinity FieldType, 
    bool IsAutoincrement = false);