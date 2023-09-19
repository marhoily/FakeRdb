namespace FakeRdb;

public sealed class FakeDb : Dictionary<string, Table>
{
    public void Insert(string tableName, 
        Func<int, int, object?> resolveValue,
        SQLiteParser.Column_nameContext[] columnNames, int rowCount)
    {
        var table = this[tableName];
        var valueSelectors = table.Schema.Columns
            .Select(field =>
            {
                var idx = Array.FindIndex(columnNames, col => col.GetText() == field.Name);
                if (idx != -1)
                {
                    return rowIndex => Convert.ChangeType(
                        resolveValue(rowIndex, idx), field.FieldType);
                }
                if (field.IsAutoincrement)
                    return new Func<int, object?>(_ => table.Autoincrement());

                return _ => Activator.CreateInstance(field.FieldType);
            })
            .ToArray();

        for (var i = 0; i < rowCount; i++)
        {
            var oneRow = valueSelectors.Select(v => v(i)).ToArray();
            table.Add(oneRow);
        }
    }
}