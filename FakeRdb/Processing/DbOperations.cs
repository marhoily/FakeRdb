namespace FakeRdb;

public static class DbOperations
{
    public static void Insert(this Database db, string tableName, string[] columns, ValuesTable values)
    {
        var table = db[tableName];
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
                        .ConvertToSqliteType(field.FieldType);

            if (field.IsAutoincrement)
                return _ => table.Autoincrement();

            return _ => field.FieldType switch
            {
                SqliteTypeAffinity.Numeric => 0,
                SqliteTypeAffinity.Integer => 0,
                SqliteTypeAffinity.Real => 0.0,
                SqliteTypeAffinity.Text => "",
                SqliteTypeAffinity.Blob => Array.Empty<byte>(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public static IResult Select(this Database db, string tableName, IProjection[] projection, IExpression? filter)
    {
        var table = db[tableName];
        var selectors = CompileProjection(table, projection);
        if (selectors.Length == 0)
            throw new InvalidOperationException(
                $"No columns selected from table: {tableName}");
        var data = BuildData(table, selectors, filter);
        var schema = BuildSchema(selectors, data);
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

        static ResultSchema BuildSchema(IEnumerable<IExpression> selectors, List<List<object?>> data)
        {
            var firstRow = data.FirstOrDefault();
            return new ResultSchema(selectors
                .Select((column, n) => new ColumnDefinition(n,
                    column.ResultName,
                    ResolveColumnType(column, firstRow, n)))
                .ToArray());
        }

        static SqliteTypeAffinity ResolveColumnType(IExpression column, List<object?>? firstRow, int n)
        {
            if (column.ExpressionType != SqliteTypeAffinity.NotSet)
                return column.ExpressionType;
            if (firstRow == null)
                return SqliteTypeAffinity.Blob;
            return firstRow[n].GetTypeAffinity();
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

    public static IResult SelectAggregate(this Database db, string tableName,
        List<FunctionCallExpression> aggregate)
    {
        var dbTable = db[tableName];
        var rows = dbTable.ToArray();
        var schema = new List<ColumnDefinition>();
        var data = new List<object?>();
        for (var i = 0; i < aggregate.Count; i++)
        {
            var func = aggregate[i];
            var cell = func.Resolve<AggregateResult>(rows);
            schema.Add(new ColumnDefinition(i,
                func.ResultName,
                cell.Value.GetSimplifyingAffinity()));
            data.Add(cell.Value);
        }

        return new QueryResult(
            new ResultSchema(schema.ToArray()),
            new List<List<object?>>
            {
                data
            });
    }


    public static int Update(this Database db, 
        string tableName,
        (string column, IExpression value)[] assignments,
        IExpression? filter)
    {
        var table = db[tableName] ?? throw new ArgumentOutOfRangeException(nameof(tableName));
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

    public static Table? Try(this Database db, string? tableName)
    {
        return tableName == null ? null : db[tableName];
    }
}