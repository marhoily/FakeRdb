namespace FakeRdb;

public sealed class FakeDb : Dictionary<string, Table>
{
    public void Insert(string tableName, string[] columns, ValuesTable values)
    {
        var table = this[tableName];

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
                return row => Convert.ChangeType(
                    values.Rows[row].Cells[col].Resolve(), 
                    field.FieldType);

            if (field.IsAutoincrement)
                return _ => table.Autoincrement();

            return _ => Activator.CreateInstance(field.FieldType);
        }
    }

    public IResult Select(string tableName, IProjection[] projection, Expression? filter)
    {
        var dbTable = this[tableName];
        var dbSchema = dbTable.Schema;
        var proj = projection is [Wildcard]
            ? Enumerable.Range(0, dbSchema.Columns.Length)
                .Select(n => new FieldAccessExpression(dbTable.Schema.Columns[n]))
                .ToArray()
            : projection.OfType<FieldAccessExpression>().ToArray();
        if (proj.Length == 0)
            throw new InvalidOperationException(
                $"No columns selected from table: {tableName}");
        var filtered = filter == null
            ? dbTable
            : dbTable.Where(x => filter.Resolve<bool>(x));
        var table = filtered
            .Select(dbRow => proj
                .Select(column => column.Resolve(dbRow))
                .ToList())
            .ToList();
        var schema = proj
            .Select(column => column.AccessedField)
            .ToArray();
        return new QueryResult(schema, table);
    }

    public IResult SelectAggregate(string tableName,
        List<FunctionCallExpression> aggregate)
    {
        var dbTable = this[tableName];
        var rows = dbTable.ToArray();
        var func = aggregate.Single();
        var result = func.Resolve<AggregateResult>(rows);
        var schema = new[]
        {
            new Field(0,
                func.ResultSetName,
                result.Value.GetRuntimeType())
        };
        return new QueryResult(schema,
            new List<List<object?>>
            {
                new() { result.Value }
            });
    }
    

    public int Update(
        string tableName,
        (string column, Expression value)[] assignments,
        Expression? filter)
    {
        var table = this[tableName] ?? throw new ArgumentOutOfRangeException(nameof(tableName));
        var schema = table.Schema;
        var compiled = assignments.Select(x =>
            (column: schema.IndexOf(x.column),
             value: x.value.BindTarget(schema[x.column])))
            .ToArray();
        var counter = 0;
        foreach (var row in table)
            if (filter == null || filter.Resolve<bool>(row))
            {
                counter++;
                foreach (var (column, value) in compiled)
                {
                    row.Data[column] = value.Resolve(row);
                }
            }
        return counter;
    }

    public Table? Try(string? tableName)
    {
        return tableName == null ? null : this[tableName];
    }
}