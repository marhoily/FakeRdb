namespace FakeRdb;

public static class TableOperations
{
    public static long Autoincrement(this Table table) => ++table.Autoincrement;
    public static void Add(this Table table, object?[] oneRow) => table.Add(new Row(table, oneRow));

}