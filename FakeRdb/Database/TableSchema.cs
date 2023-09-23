namespace FakeRdb;

public sealed record TableSchema(Column[] Columns)
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public Column Get(string columnName)
    {
        return Array.Find(Columns, f => string.Equals(f.Name, columnName, IgnoreCase)) ??
               throw Exceptions.ColumnNotFound(columnName);
    }
    public Column? TryGet(string columnName)
    {
        return Array.Find(Columns, f => string.Equals(f.Name, columnName, IgnoreCase));
    }


    public int IndexOf(string columnName)
    {
        var result = Array.FindIndex(Columns, 
            column => string.Equals(column.Name, columnName, IgnoreCase));
        if (result == -1)
            throw Exceptions.ColumnNotFound(columnName);
        return result;
    }

}