namespace FakeRdb;

public sealed record Field(int ColumnIndex, string Name, 
    string DataType,
    Type FieldType, 
    bool IsAutoincrement = false);