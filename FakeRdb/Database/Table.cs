namespace FakeRdb;

public sealed class Table : List<Row>
{
    public static readonly Table Empty =
        new(new TableSchema(Array.Empty<ColumnHeader>()));
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
    public void AddRows(IEnumerable<Row> rows)
    {
        foreach (var r in rows)
            Add(r);
    }

    public IEnumerable<Row> GetRows()
    {
        for (var i = 0; i < Data[0].Count; i++)
        {
            var row = new object?[Data.Length];
            for (var j = 0; j < Data.Length; j++)
            {
                row[j] = Data[j][i];
            }
            yield return new Row(row);
        }
    }

    public Table ConcatColumns(Table table)
    {
        return new Table(new TableSchema(
            Schema.Columns.Concat(table.Schema.Columns)
                .ToArray()));
        
    }

   
}