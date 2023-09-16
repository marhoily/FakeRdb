namespace FakeRdb;

public sealed record Field(string Name, Type FieldType, bool IsAutoincrement = false);