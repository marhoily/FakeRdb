namespace FakeRdb;

public static class SchemaOperations
{
    private const StringComparison NameRule = StringComparison.InvariantCultureIgnoreCase;

    public static Field Get(this TableSchema schema, string fieldName)
    {
        return Array.Find(schema.Columns, f => string.Equals(f.Name, fieldName, NameRule)) ??
               throw FieldNotFound(fieldName);
    }

    private static InvalidOperationException FieldNotFound(string name)
    {
        return new InvalidOperationException($"Column {name} is not found");
    }

    public static int IndexOf(this TableSchema schema, string columnName)
    {
        var result = Array.FindIndex(schema.Columns, 
            field => string.Equals(field.Name, columnName, NameRule));
        if (result == -1)
            throw FieldNotFound(columnName);
        return result;
    }

    public static int IndexOf(this ResultSchema schema, Field field) =>
        schema.IndexOf(field.Name);
    public static int IndexOf(this ResultSchema schema, string columnName)
    {
        var result = Array.FindIndex(schema.Columns, 
            field => string.Equals(field.Name, columnName, NameRule));
        if (result == -1)
            throw FieldNotFound(columnName);
        return result;
    }
}