namespace FakeRdb;

public sealed class TableSchema
{
    private const StringComparison NameRule = StringComparison.InvariantCultureIgnoreCase;
    public Field[] Columns { get; }

    public TableSchema(Field[] columns)
    {
        Columns = columns;
    }
    public Field this [string name] => 
        Array.Find(Columns, f => string.Equals(f.Name, name, NameRule)) ??
        throw FieldNotFound(name);

    private static InvalidOperationException FieldNotFound(string name)
    {
        return new InvalidOperationException($"Column {name} is not found");
    }

    public int IndexOf(string columnName)
    {
        var result = Array.FindIndex(Columns, 
            field => string.Equals(field.Name, columnName, NameRule));
        if (result == -1)
            throw FieldNotFound(columnName);
        return result;
    }
}