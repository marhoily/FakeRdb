namespace FakeRdb;

public sealed class FakeDb : Dictionary<string, Table>
{
    public void Insert(string tableName, string[] columns, ValuesTable values)
    {
        var table = this[tableName];
        if (columns.Length == 0) // TODO: Should it be here or in outer method? Is there more efficient way?
            columns = table.Schema.Columns.Select(c => c.Name).ToArray();

        var columnGenerator = table.Schema.Columns
            .Select(PrepareColumnValueGenerator)
            .ToArray();

        for (var i = 0; i < values.Rows.Length; i++)
            table.Add(columnGenerator.Select(gen => gen(i)).ToArray());

        return;

        Func<int, object?> PrepareColumnValueGenerator(Field field)
        {
            var col = Array.IndexOf(columns, field.Name);
            if (col != -1)
                return row =>
                    values.Rows[row].Cells[col]
                        .Eval()
                        .Coerce(field.FieldType);

            if (field.IsAutoincrement)
                return _ => table.Autoincrement();

            return _ => field.FieldType switch
            {
                SqliteTypeAffinity.Numeric => 0,
                SqliteTypeAffinity.Integer => 0,
                SqliteTypeAffinity.Real => 0,
                SqliteTypeAffinity.Text => "",
                SqliteTypeAffinity.Blob => Array.Empty<byte>(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public IResult Select(string tableName, IProjection[] projection, IExpression? filter)
    {
        var table = this[tableName];
        var selectors = CompileProjection(table, projection);
        if (selectors.Length == 0)
            throw new InvalidOperationException(
                $"No columns selected from table: {tableName}");
        var data = BuildData(table, selectors, filter);
        var schema = BuildSchema(selectors);
        return new QueryResult(schema, data);

        static IExpression[] CompileProjection(Table table, IProjection[] projection)
        {
            if (projection is [Wildcard])
            {
                return Enumerable.Range(0, table.Schema.Columns.Length)
                    .Select(n => new ProjectionExpression(table.Schema.Columns[n]))
                    .Cast<IExpression>()
                    .ToArray();
            }

            return projection.OfType<IExpression>().ToArray();
        }

        static List<List<object?>> BuildData(Table source, IExpression[] proj, IExpression? filter)
        {
            var temp = ApplyFilter(source, filter);
            return ApplyProjection(temp, proj);
        }

        static Field[] BuildSchema(IEnumerable<IExpression> selectors)
        {
            return selectors
                .Select((column, n) => new Field(n, column.ResultName, column.ExpressionType))
                .ToArray();
        }

        static IEnumerable<Row> ApplyFilter(Table source, IExpression? expression)
        {
            return expression == null
                ? source
                : source.Where(expression.Resolve<bool>);
        }

        static List<List<object?>> ApplyProjection(IEnumerable<Row> rows, IExpression[] selectors)
        {
            return rows
                .Select(row => selectors
                    .Select(selector => selector.Eval(row))
                    .ToList())
                .ToList();
        }
    }

    public IResult SelectAggregate(string tableName,
        List<FunctionCallExpression> aggregate)
    {
        var dbTable = this[tableName];
        var rows = dbTable.ToArray();
        var schema = new List<Field>();
        var data = new List<object?>();
        for (var i = 0; i < aggregate.Count; i++)
        {
            var func = aggregate[i];
            var cell = func.Resolve<AggregateResult>(rows);
            schema.Add(new Field(i,
                func.ResultName,
                    cell.Value.GetTypeAffinity()));
            data.Add(cell.Value);
        }

        return new QueryResult(schema.ToArray(),
            new List<List<object?>>
            {
                data
            });
    }


    public int Update(
        string tableName,
        (string column, IExpression value)[] assignments,
        IExpression? filter)
    {
        var table = this[tableName] ?? throw new ArgumentOutOfRangeException(nameof(tableName));
        var schema = table.Schema;
        var compiled = assignments.Select(x =>
            (column: schema.IndexOf(x.column), x.value))
            .ToArray();
        var counter = 0;
        foreach (var row in table)
            if (filter == null || filter.Resolve<bool>(row))
            {
                counter++;
                foreach (var (column, value) in compiled)
                {
                    row.Data[column] = value.Eval(row)
                        .Coerce(schema.Columns[column].FieldType);
                }
            }
        return counter;
    }

    public Table? Try(string? tableName)
    {
        return tableName == null ? null : this[tableName];
    }
}