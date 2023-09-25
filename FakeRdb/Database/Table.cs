namespace FakeRdb;

public sealed class Table : List<Row>
{
    public TableSchema Schema { get; }
    public Column[] Data { get; }

    private int _autoincrement;

    public Table(TableSchema schema)
    {
        Schema = schema;
        Data = schema.Columns
            .Select(col => new Column(col))
            .ToArray();
    }

    public long Autoincrement() => ++_autoincrement;
    public void Add(object?[] oneRow)
    {
        Add(new Row(oneRow));
        for (var i = 0; i < oneRow.Length; i++) 
            Data[i].Add(oneRow[i]);
    }
}