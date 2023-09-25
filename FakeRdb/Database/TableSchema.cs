namespace FakeRdb;

public sealed record TableSchema(ColumnHeader[] Columns)
{
    private const StringComparison IgnoreCase = StringComparison.InvariantCultureIgnoreCase;

    public ColumnHeader Get(string columnName)
    {
        return Array.Find(Columns, f => string.Equals(f.Name, columnName, IgnoreCase)) ??
               throw Resources.ColumnNotFound(columnName);
    }
    public ColumnHeader? TryGet(string columnName)
    {
        return Array.Find(Columns, f => string.Equals(f.Name, columnName, IgnoreCase));
    }


    public int IndexOf(string columnName)
    {
        var result = Array.FindIndex(Columns, 
            column => string.Equals(column.Name, columnName, IgnoreCase));
        if (result == -1)
            throw Resources.ColumnNotFound(columnName);
        return result;
    }

}