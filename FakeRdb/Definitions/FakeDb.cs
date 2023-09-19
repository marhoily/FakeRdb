namespace FakeRdb;

public sealed class FakeDb : Dictionary<string, Table>
{
    public void Insert(string tableName, 
        SQLiteParser.Value_rowContext[] sqlRows,
        SQLiteParser.Column_nameContext[] columnNames, 
        FakeDbParameterCollection parameters)
    {
        var table = this[tableName];
        var valueSelectors = table.Schema.Columns
            .Select(field =>
            {
                var idx = Array.FindIndex(columnNames, col => col.GetText() == field.Name);
                if (idx != -1)
                {
                    return rowIndex => sqlRows[rowIndex].expr(idx).Resolve(parameters, field.FieldType);
                }
                if (field.IsAutoincrement)
                    return new Func<int, object?>(_ => table.Autoincrement());

                return _ => Activator.CreateInstance(field.FieldType);
            })
            .ToArray();

        for (var i = 0; i < sqlRows.Length; i++)
        {
            var oneRow = valueSelectors.Select(v => v(i)).ToArray();
            table.Add(oneRow);
        }
    }
}