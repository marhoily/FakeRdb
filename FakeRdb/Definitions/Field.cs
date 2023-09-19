namespace FakeRdb;

public sealed record Field(int ColumnIndex, string Name, 
    DynamicType FieldType, 
    bool IsAutoincrement = false);