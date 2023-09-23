namespace FakeRdb;

public sealed class Table : List<Row>
{
    public TableSchema Schema { get; }
    public int Autoincrement { get; set; }

    public Table(TableSchema schema) => Schema = schema;
}