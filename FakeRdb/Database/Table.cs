using static FakeRdb.IR;

namespace FakeRdb;

public sealed class Table : IResult
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public static readonly Table Empty =
        new(Array.Empty<ColumnHeader>());
    public Column[] Columns { get; }
    public IEnumerable<ColumnHeader> Headers => Columns.Select(c => c.Header);

    private int _autoincrement;

    public Table(Column[] columns)
    {
        Columns = columns;
    }

    public Table(IEnumerable<ColumnHeader> columns)
    {
        Columns = columns
            .Select(col => new Column(col, new List<object?>()))
            .ToArray();
    }

    public long Autoincrement() => ++_autoincrement;

    public void Add(params object?[] oneRow)
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
    public void AddRows(IEnumerable<IEnumerable<object?>> rows)
    {
        foreach (var row in rows)
            foreach (var (dest, src) in Columns.Zip(row))
                dest.Rows.Add(src);
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
        return new Table(Headers.Concat(table.Headers).ToArray());
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
        var result = new Table(Headers);
        result.AddRows(GetRows());
        return result;
    }

    public Table OrderBy(OrderingTerm[] orderingTerms)
    {
        var result = this;
        foreach (var orderingTerm in orderingTerms)
        {
            var comparer = Row.Comparer(orderingTerm.Column.ColumnIndex);
            var derived = new Table(Headers);
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
    public Column Get(string columnName)
    {
        return Array.Find(Columns, f => string.Equals(f.Header.Name, columnName, IgnoreCase))
            ?? throw Resources.ColumnNotFound(columnName);
    }

    public int IndexOf(string columnName)
    {
        var result = Array.FindIndex(Columns,
            column => string.Equals(column.Header.Name, columnName, IgnoreCase));
        if (result == -1)
            throw Resources.ColumnNotFound(columnName);
        return result;
    }

    public Table Project(ResultColumn[] columns)
    {
        var result = new List<Column>();
        for (var index = 0; index < columns.Length; index++)
        {
            var column = columns[index];
            result.Add(column.Exp switch
            {
                ColumnExp columnExp => Get(columnExp.Value.Header.Name).Derive(column.Alias),
                _ => ToColumn(index, column)
            });
        }

        return new Table(result.ToArray());
        Column ToColumn(int index, ResultColumn col)
        {
            var data = new List<object?>();
            for (var i = 0; i < RowCount; i++)
            {
                data.Add(col.Exp.Eval(this, i));
            }

            var name = col.Alias ?? col.Original;
            var type = data[0].GetTypeAffinity();
            return new Column(new ColumnHeader(index, name, type), data);
        }
    }

    public List<List<object?>> ToList()
    {

        return GetRows().Select(r => r.Data.ToList()).ToList();
    }

    public override string ToString()
    {
        return Utils.PrettyPrint.Table(
            Headers.Select(col => $"{col.Name} : {col.ColumnType}").ToList(),
            ToList());
    }
    public Table GroupBy(Column[] columns, ResultColumn[] projection)
    {
        var rows = Enumerable.Range(0, RowCount)
            .GroupBy(rowIndex => new Row.CompositeKey(
                columns.Select(c => c.Rows[rowIndex]).ToArray()))
            .OrderBy(g => g.Key) // Required to mimic SQLite's grouping behavior
            .Select(g => projection.Select(col => col.Exp switch
            {
                AggregateExp agg => agg.Function.Invoke(
                    g.Select(GetRow).ToArray(), agg.Args),
                var otherExp => otherExp.Eval(this, g.First())
            }))
            .ToArray();
        var result = new Table(projection.Zip(rows.First())
            .Select((col, n) => new ColumnHeader(n,
                col.First.Alias ?? col.First.Original, 
                col.Second.CalculateEffectiveAffinity())));

        result.AddRows(rows);
        return result;
    }

    private static void ValidateSchema(Column[] x, Column[] y)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException("SELECTs to the left and right of UNION do not have the same number of result columns");
        }
    }
    public static Table Union(Table x, Table y)
    {
        ValidateSchema(x.Columns, y.Columns);

        // Use Distinct to remove duplicates. This assumes that List<object?> implements appropriate equality semantics.
        var resultData = x.GetRows().Concat(y.GetRows())
            .Distinct(Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();

        var result = new Table(x.Headers);
        result.AddRows(resultData);
        return result;
    }
    public static Table Intersect(Table x, Table y)
    {
        ValidateSchema(x.Columns, y.Columns);
        var resultData = x.GetRows()
            .Intersect(y.GetRows(), Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();
        var result = new Table(x.Headers);
        result.AddRows(resultData);
        return result;
    }
    public static Table Except(Table x, Table y)
    {
        ValidateSchema(x.Columns, y.Columns);

        var resultData = x.GetRows()
            .Except(y.GetRows(), Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();

        var result = new Table(x.Headers);
        result.AddRows(resultData);
        return result;
    }
    public static Table UnionAll(Table x, Table y)
    {
        ValidateSchema(x.Columns, y.Columns);

        var resultData = x.GetRows().ToList();
        resultData.AddRange(y.GetRows());

        var result = new Table(x.Headers);
        result.AddRows(resultData);
        return result;
    }
    public Table ResolveColumnTypes()
    {
        return new Table(Columns.Select(col =>
        {
            if (col.Header.ColumnType != TypeAffinity.NotSet) return col;
            var affinity = col.Rows.FirstOrDefault().GetTypeAffinity();
            var newHeader = col.Header with { ColumnType = affinity };
            return col with { Header = newHeader };
        }).ToArray());
    }
}