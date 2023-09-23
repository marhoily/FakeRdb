namespace FakeRdb;

public static class SchemaOperations
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public static Column Get(this TableSchema schema, string columnName)
    {
        return Array.Find(schema.Columns, f => string.Equals(f.Name, columnName, IgnoreCase)) ??
               throw ColumnNotFound(columnName);
    }
    public static Column? TryGet(this TableSchema schema, string columnName)
    {
        return Array.Find(schema.Columns, f => string.Equals(f.Name, columnName, IgnoreCase));
    }

    public static InvalidOperationException ColumnNotFound(string name)
    {
        return new InvalidOperationException($"Column {name} is not found");
    }

    public static int IndexOf(this TableSchema schema, string columnName)
    {
        var result = Array.FindIndex(schema.Columns, 
            column => string.Equals(column.Name, columnName, IgnoreCase));
        if (result == -1)
            throw ColumnNotFound(columnName);
        return result;
    }

    public static int IndexOf(this ResultSchema schema, Column column) =>
        schema.IndexOf(column.Name);
    public static int IndexOf(this ResultSchema schema, string columnName)
    {
        var result = Array.FindIndex(schema.Columns, 
            col => string.Equals(col.Name, columnName, IgnoreCase));
        if (result == -1)
            throw ColumnNotFound(columnName);
        return result;
    }
}