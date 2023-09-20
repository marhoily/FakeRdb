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
        var dbTable = this[tableName];
        var dbSchema = dbTable.Schema;
        var proj = BuildProjection();
        var filtered = filter == null
            ? dbTable
            : dbTable.Where(x => filter.Resolve<bool>(x));
        var data = filtered
            .Select(dbRow => proj
                .Select(column => column.Eval(dbRow))
                .ToList())
            .ToList();
        var schema = proj
            .Select((exp, n) => new Field(n, exp.ResultName, exp.ExpressionType))
            .ToArray();
        return new QueryResult(schema, data);

        IExpression[] BuildProjection()
        {
            if (projection is [Wildcard])
            {
                var all = Enumerable.Range(0, dbSchema.Columns.Length)
                    .Select(n => new ProjectionExpression(dbTable.Schema.Columns[n]))
                    .Cast<IExpression>()
                    .ToArray();
                return all;
            }

            var result = projection.OfType<IExpression>().ToArray();
            if (result.Length == 0)
                throw new InvalidOperationException(
                    $"No columns selected from table: {tableName}");
            return result;
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