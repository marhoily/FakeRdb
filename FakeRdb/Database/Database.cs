namespace FakeRdb;

public sealed class Database : Dictionary<string, Table>
{

}

public sealed class Table : List<Row>
{
    public TableSchema Schema { get; }
    public int Autoincrement { get; set; }

    public Table(TableSchema schema) => Schema = schema;
}

public sealed record Row(object?[] Data)
{
    public object? this[Field field] => Data[field.ColumnIndex];
}

public sealed record TableSchema(Field[] Columns);

public sealed record Field(int ColumnIndex, string Name, 
    TypeAffinity FieldType, bool IsAutoincrement = false);