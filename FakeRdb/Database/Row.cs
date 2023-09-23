namespace FakeRdb;

public sealed record Row(object?[] Data)
{
    public object? this[Column column] => Data[column.ColumnIndex];
}