namespace FakeRdb;

public sealed record Field(string Name, 
    string DataType,
    Type FieldType, bool IsAutoincrement = false);