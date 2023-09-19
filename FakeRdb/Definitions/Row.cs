namespace FakeRdb;

public sealed record Row(Table Table, object?[] Data)
{
    public object? this[Field field] => Data[field.ColumnIndex];
}