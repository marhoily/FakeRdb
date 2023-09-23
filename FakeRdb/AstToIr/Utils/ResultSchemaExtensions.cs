namespace FakeRdb;

public static class ResultSchemaExtensions
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public static int IndexOf(this ResultSchema schema, Column column) =>
        schema.IndexOf(column.Name);
    public static int IndexOf(this ResultSchema schema, string columnName)
    {
        var result = Array.FindIndex(schema.Columns, 
            col => string.Equals(col.Name, columnName, IgnoreCase));
        if (result == -1)
            throw Exceptions.ColumnNotFound(columnName);
        return result;
    }
}