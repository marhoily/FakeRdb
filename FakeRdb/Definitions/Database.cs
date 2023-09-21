namespace FakeRdb;

public sealed class Database : Dictionary<string, Table>
{

}

public sealed class Table : List<Row>
{
    public int Autoincrement { get; set; }

    public Table(TableSchema schema)
    {
        Schema = schema;
    }

    public TableSchema Schema { get; }
}

public sealed record Row(Table Table, object?[] Data)
{
    public object? this[Field field] => Data[field.ColumnIndex];
}

public sealed class TableSchema
{
    public Field[] Columns { get; }

    public TableSchema(Field[] columns)
    {
        Columns = columns;
    }

}
public sealed record Field(int ColumnIndex, string Name, 
    SqliteTypeAffinity FieldType, 
    bool IsAutoincrement = false);