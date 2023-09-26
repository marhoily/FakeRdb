using static FakeRdb.IR;

namespace FakeRdb;

public sealed class Table : IResult
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public static readonly Table Empty =
        new("", Array.Empty<ColumnHeader>());

    public string Name { get; }
    public Column[] Columns { get; }
    public IEnumerable<ColumnHeader> Headers => Columns.Select(c => c.Header);

    private int _autoincrement;

    public Table(string name, Column[] columns)
    {
        columns.Select(c => c.Rows.Count).AssertAreAllEqual();
        this.Name = name;
        Columns = columns;
    }

    public Table(string name, IEnumerable<ColumnHeader> columns)
    {
        Name = name;
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

    public Table WithRows(IEnumerable<Row> rows)
    {
        foreach (var r in rows)
            for (var i = 0; i < r.Data.Length; i++)
                Columns[i].Rows.Add(r.Data[i]);
        return this;
    }
    public Table WithRows(IEnumerable<IEnumerable<object?>> rows)
    {
        foreach (var row in rows)
            foreach (var (dest, src) in Columns.Zip(row))
                dest.Rows.Add(src);
        return this;
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

    public Table ConcatHeaders(Table table)
    {
        return new Table(Name, Headers.Concat(table.Headers).ToArray());
    }

    public Table Concat(Table table)
    {
        return new Table(Name, Columns.Concat(table.Columns).ToArray());
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
    public int RemoveAll(Func<int, bool> predicate)
    {
        var counter = 0;
        var list = GetRows().ToList();
        for (var rowIndex = list.Count - 1; rowIndex >= 0; rowIndex--)
        {
            if (!predicate(rowIndex)) continue;
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
        RemoveAll(row => !filter.Eval<bool>(this, row));
    }

    public Table Clone()
    {
        return new Table(Name, Headers).WithRows(GetRows());
    }

    public Table OrderBy(OrderingTerm[] orderingTerms)
    {
        var result = this;
        foreach (var orderingTerm in orderingTerms)
        {
            var columnIndex = IndexOf(orderingTerm.Column.Name);
            result = new Table(Name, Headers)
                .WithRows(Enumerable
                    .Range(0, RowCount)
                    .OrderBy(GetRow, Row.Comparer(columnIndex))
                    .Select(GetRow));
        }

        return result;
    }

    public Column? TryGet(string columnName)
    {
        return Array.Find(Columns, f => string.Equals(
                   f.Header.FullName, columnName, IgnoreCase)) ??
               Array.Find(Columns, f => string.Equals(
                   f.Header.Name, columnName, IgnoreCase));
    }
    public Column Get(string columnName)
    {
        return TryGet(columnName) ?? throw Resources.ColumnNotFound(columnName);
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
        for (var i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            var id = column.Alias ?? column.Original;
            result.Add(column.Exp switch
            {
                ColumnExp col => Get(col.Value.Header.FullName).Derive(column.Alias),
                AggregateExp => Get(id),
                _ => ToColumn(i, column)
            });
        }

        return new Table(Name, result.ToArray());
        Column ToColumn(int index, ResultColumn col)
        {
            var data = new List<object?>();
            for (var i = 0; i < RowCount; i++)
            {
                data.Add(col.Exp.Eval(this, i));
            }

            var name = col.Alias ?? col.Original;
            var type = data[0].GetTypeAffinity();
            return new Column(new ColumnHeader(index, name, Name + "." + name, type), data);
        }
    }

    public List<List<object?>> ToList()
    {

        return GetRows().Select(r => r.Data.ToList()).ToList();
    }

    public string Print => PrettyPrint.Table(
            Headers.Select(col => $"{col.Name} : {col.ColumnType}").ToList(),
            ToList());

    public Table GroupBy(Column[] columns, ResultColumn[] projection)
    {
        if (columns.Length == 0 && !projection.Any(col => col.Exp is AggregateExp))
            return this;

        var groups = Enumerable.Range(0, RowCount)
            .GroupBy(rowIndex => new Row.CompositeKey(
                columns.Select(c => c.Rows[rowIndex]).ToArray()))
            .OrderBy(g => g.Key)  // Required to mimic grouping behavior of Sqlite
            .ToList();
        var rows = groups
            .Select(g => projection.Select(col => col.Exp switch
                {
                    AggregateExp agg => agg.Function.Invoke(
                        g.Select(GetRow).ToArray(), agg.Args),
                    var otherExp => otherExp.Eval(this, g.First())
                }))
            .ToArray();
        var nativeColumns = new Table(Name, Columns
            .Select(col => col with
            {
                Rows = groups
                    .Select(g => col.Rows[g.First()])
                    .ToList()
            }).ToArray());
        var columnHeaders = rows.Length == 0
            ? projection.Select((col, n) => new ColumnHeader(n,
                col.Alias ?? col.Original, null, TypeAffinity.NotSet))
            : projection.Zip(rows.First())
                .Select((col, n) => new ColumnHeader(n,
                    col.First.Alias ?? col.First.Original,
                    null,
                    col.Second.CalculateEffectiveAffinity()));
        return nativeColumns.Concat(
            new Table(Name, columnHeaders).WithRows(rows));
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

        return new Table("???", x.Headers).WithRows(resultData);
    }
    public static Table Intersect(Table x, Table y)
    {
        ValidateSchema(x.Columns, y.Columns);
        var resultData = x.GetRows()
            .Intersect(y.GetRows(), Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();
        return new Table("???", x.Headers).WithRows(resultData);
    }
    public static Table Except(Table x, Table y)
    {
        ValidateSchema(x.Columns, y.Columns);

        var resultData = x.GetRows()
            .Except(y.GetRows(), Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();

        return new Table("???", x.Headers).WithRows(resultData);
    }
    public static Table UnionAll(Table x, Table y)
    {
        ValidateSchema(x.Columns, y.Columns);

        var resultData = x.GetRows().ToList();
        resultData.AddRange(y.GetRows());

        return new Table("???", x.Headers).WithRows(resultData);

    }
    public Table ResolveColumnTypes()
    {
        return new Table(Name, Columns.Select(col =>
        {
            if (col.Header.ColumnType != TypeAffinity.NotSet) return col;
            var affinity = col.Rows.FirstOrDefault().GetTypeAffinity();
            var newHeader = col.Header with { ColumnType = affinity };
            return col with { Header = newHeader };
        }).ToArray());
    }
}