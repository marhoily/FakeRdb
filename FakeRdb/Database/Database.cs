using static FakeRdb.IR;

namespace FakeRdb;

public sealed class Database : Dictionary<string, Table>
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

        Func<int, object?> PrepareColumnValueGenerator(ColumnHeader column)
        {
            var col = Array.IndexOf(columns, column.Name);
            if (col != -1)
                return row =>
                    values.Rows[row].Cells[col]
                        .Eval()
                        .ConvertToSqliteType(column.ColumnType);

            if (column.IsAutoincrement)
                return _ => table.Autoincrement();

            return _ => column.ColumnType switch
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
        for (var rowIndex = 0; rowIndex < table.Count; rowIndex++)
        {
            var row = table.GetRow(rowIndex);
            if (filter != null && !filter.Eval<bool>(row)) continue;
            counter++;
            foreach (var (column, value) in compiled)
            {
                table.Set(rowIndex, column, value.Eval(row)
                    .Coerce(schema.Columns[column].ColumnType));
            }
        }

        return counter;
    }
    public int Delete(string tableName, IExpression? projection)
    {
        var table = this[tableName];
        if (projection != null)
            return table.RemoveAll(projection.Eval<bool>);

        var affected = table.Data[0].Count;
        foreach (var column in table.Data)
        {
            column.Clear();
        }
        return affected;
    }

    public Table? Try(string? tableName)
    {
        return tableName == null ? null : this[tableName];
    }
    public Table[]? TrySet(string? tableName)
    {
        return tableName == null ? null : new[] { this[tableName] };
    }
}