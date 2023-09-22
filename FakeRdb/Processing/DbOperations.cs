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
                TypeAffinity.Numeric => 0,
                TypeAffinity.Integer => 0,
                TypeAffinity.Real => 0.0,
                TypeAffinity.Text => "",
                TypeAffinity.Blob => Array.Empty<byte>(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public static QueryResult Select(this Table from, IR.ResultColumn[] projection, IExpression? filter)
    {
        var selectors = projection.Select(c => c.Exp.Convert()).ToArray();
        var data = BuildData(from, selectors, filter);
        var schema = BuildSchema(projection, selectors);
        return new QueryResult(schema, data);

        static List<List<object?>> BuildData(Table source, IExpression[] proj, IExpression? filter)
        {
            var temp = ApplyFilter(source, filter);
            return ApplyProjection(temp, proj);
        }

        static ResultSchema BuildSchema(IR.ResultColumn[] columns, IExpression[] expressions)
        {
            return new ResultSchema(columns.Zip(expressions)
                .Select(column => new ColumnDefinition(
                    column.First.Alias ??
                    ExtractColumnName(column.First.Exp) ??
                    column.First.Original ,
                    column.Second.ExpressionType))
                .ToArray());
            static string? ExtractColumnName(IR.IExpression exp)
            {
                if (exp is IR.ColumnExp col) return col.Value.Name;
                return null;
            }
        }

        static IEnumerable<Row> ApplyFilter(Table source, IExpression? expression)
        {
            return expression == null
                ? source
                : source.Where(expression.Eval<bool>);
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

    public static QueryResult SelectAggregate(this Table from, List<IR.ResultColumn> aggregate)
    {
        var rows = from.ToArray();
        var schema = new List<ColumnDefinition>();
        var data = new List<object?>();
        foreach (var resultColumn in aggregate)
        {
            var cell = resultColumn.Exp.Convert()
                .Eval<AggregateResult>(rows);
            schema.Add(new ColumnDefinition(resultColumn.Original,
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
            if (filter == null || filter.Eval<bool>(row))
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
    public static int Delete(this Database db, string tableName, IExpression? projection)
    {
        var table = db[tableName];
        if (projection != null) 
            return table.RemoveAll(projection.Eval<bool>);

        var affected = table.Count;
        table.Clear();
        return affected;
    }

    public static Table? Try(this Database db, string? tableName)
    {
        return tableName == null ? null : db[tableName];
    }
}