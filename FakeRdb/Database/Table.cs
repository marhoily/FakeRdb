namespace FakeRdb;

public sealed class Table
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
        for (var i = 0; i < oneRow.Length; i++) 
            Data[i].Add(oneRow[i]);
    }
    public void AddRows(IEnumerable<Row> rows)
    {
        foreach (var r in rows)
            for (var i = 0; i < r.Data.Length; i++) 
                Data[i].Add(r.Data[i]);
    }

    public int Count => Data[0].Count;
    public IEnumerable<Row> GetRows()
    {
        for (var i = 0; i < Count; i++)
            yield return GetRow(i);
    }
    public Row GetRow(int rowIndex)
    {
        var row = new object?[Data.Length];
        for (var j = 0; j < Data.Length; j++)
        {
            row[j] = Data[j][rowIndex];
        }
        return new Row(row);
    }

    public Table ConcatColumns(Table table)
    {
        return new Table(new TableSchema(
            Schema.Columns.Concat(table.Schema.Columns)
                .ToArray()));
    }

    public IEnumerable<IGrouping<Row.CompositeKey, Row>> GroupBy(Func<Row, Row.CompositeKey> keySelector)
    {
        return GetRows().GroupBy(keySelector);
    }

    public int RemoveAll(Func<Row, bool> predicate)
    {
        var counter = 0;
        var list = GetRows().ToList();
        for (var i = 0; i < list.Count; i++)
        {
            if (!predicate(list[i])) continue;
            RemoveAt(i);
            counter++;
        }
        return counter;
    }

    private void RemoveAt(int rowIndex)
    {
        foreach (var column in Data)
        {
            column.RemoveAt(rowIndex);
        }
    }


    public void Set(int rowIndex, int columnIndex, object? value)
    {
        Data[columnIndex][rowIndex] = value;
    }
}