namespace FakeRdb;

public sealed class FakeDb : Dictionary<string, Table>
{
    public void Insert(string tableName,
        string[] columns,
        Func<int, int, object?> cells,
        int rowCount)
    {
        var table = this[tableName];

        var columnGenerator = table.Schema.Columns
            .Select(PrepareColumnValueGenerator)
            .ToArray();

        for (var i = 0; i < rowCount; i++) 
            table.Add(columnGenerator.Select(gen => gen(i)).ToArray());

        return;

        Func<int, object?> PrepareColumnValueGenerator(Field field)
        {
            var col = Array.IndexOf(columns, field.Name);
            if (col != -1)
                return row => Convert.ChangeType(
                    cells(row, col), field.FieldType);

            if (field.IsAutoincrement) 
                return _ => table.Autoincrement();

            return _ => Activator.CreateInstance(field.FieldType);
        }
    }

    public FakeDbReader Select(string tableName, string[] projection)
    {
        var dbTable = this[tableName];
        var dbSchema = dbTable.Schema;
        var proj = projection is ["*"]
            ? Enumerable.Range(0, dbSchema.Columns.Length).ToArray()
            : projection.Select(col => dbSchema.IndexOf(col)).ToArray();
        if (proj.Length == 0)
            throw new InvalidOperationException(
                $"No columns selected from table: {tableName}");
        var table = dbTable
            .Select(dbRow => proj
                .Select(column => dbRow.Data[column])
                .ToList())
            .ToList();
        var schema = proj
            .Select(column => dbSchema.Columns[column])
            .ToArray();
        return new FakeDbReader(
            new QueryResult(schema, table));
    }
}