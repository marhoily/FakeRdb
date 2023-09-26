using static FakeRdb.IR;

namespace FakeRdb;

public sealed class Table
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public static readonly Table Empty =
        new(new TableSchema(Array.Empty<ColumnHeader>()));
    public TableSchema Schema { get; }
    public Column[] Columns { get; }

    private int _autoincrement;

    public Table(TableSchema schema)
    {
        Schema = schema;
        Columns = schema.Columns
            .Select(col => new Column(col, new List<object?>()))
            .ToArray();
    }

    public long Autoincrement() => ++_autoincrement;
    public void Add(object?[] oneRow)
    {
        for (var i = 0; i < oneRow.Length; i++) 
            Columns[i].Rows.Add(oneRow[i]);
    }
    public void AddRows(IEnumerable<Row> rows)
    {
        foreach (var r in rows)
            for (var i = 0; i < r.Data.Length; i++) 
                Columns[i].Rows.Add(r.Data[i]);
    }

    public int RowCount => Columns[0].Rows.Count;
    public IEnumerable<Row> GetRows()
    {
        for (var i = 0; i < RowCount; i++)
            yield return GetRow(i);
    }
    public Row GetRow(int rowIndex)
    {
        var row = new object?[Columns.Length];
        for (var j = 0; j < Columns.Length; j++)
        {
            row[j] = Columns[j].Rows[rowIndex];
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
        for (var rowIndex = list.Count - 1; rowIndex >= 0; rowIndex--)
        {
            if (!predicate(list[rowIndex])) continue;
            RemoveAt(rowIndex);
            counter++;
        }
        return counter;
    }

    private void RemoveAt(int rowIndex)
    {
        foreach (var column in Columns)
        {
            column.Rows.RemoveAt(rowIndex);
        }
    }


    public void Set(int rowIndex, int columnIndex, object? value)
    {
        Columns[columnIndex].Rows[rowIndex] = value;
    }

    public void ApplyFilter(IExpression filter)
    {
        RemoveAll(row => !filter.Eval<bool>(row));
    }

    public Table Clone()
    {
        var result = new Table(Schema);
        result.AddRows(GetRows());
        return result;
    }

    public Table OrderBy(OrderingTerm[] orderingTerms)
    {
        var result = this;
        foreach (var orderingTerm in orderingTerms)
        {
            var comparer = Row.Comparer(
                orderingTerm.Column.ColumnIndex);
            var derived = new Table(Schema);
            derived.AddRows(
                Enumerable.Range(0, RowCount)
                    .OrderBy(GetRow, comparer)
                    .Select(GetRow));
            result = derived;
        }

        return result;
    }

    public Column? TryGet(string columnName)
    {
        return Array.Find(Columns, f => string.Equals(f.Header.Name, columnName, IgnoreCase));
    }
}