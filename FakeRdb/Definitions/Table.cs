namespace FakeRdb;

public sealed class Table : List<Row>
{
    private int _autoincrement;

    public Table(TableSchema schema)
    {
        Schema = schema;
    }

    public long Autoincrement() => ++_autoincrement;
    public TableSchema Schema { get; }

    public void Add(object?[] oneRow) => Add(new Row(this, oneRow));

}