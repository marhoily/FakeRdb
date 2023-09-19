namespace FakeRdb;

public sealed class FakeDb : Dictionary<string, Table>
{
    public void Insert(string tableName, 
        Func<int, int, object?> cellValue,
        string[] columns,
        int rowCount)
    {
        var table = this[tableName];
        var valueSelectors = table.Schema.Columns
            .Select(field =>
            {
                var col = Array.IndexOf(columns, field.Name);
                if (col != -1)
                {
                    return row => Convert.ChangeType(
                        cellValue(row, col), field.FieldType);
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