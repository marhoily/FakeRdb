namespace FakeRdb;

public static class AggregateSelectExecutor
{
    public static QueryResult SelectAggregate(Table table, IR.ResultColumn[] projection, ColumnHeader[] groupBy)
    {
        var data = table.GroupBy(
            groupBy.Select(c => table.Columns[c.ColumnIndex]).ToArray(),
            projection);
        return new QueryResult(new ResultSchema(
            data.Schema.Select(c => c.ToDefinition()).ToArray()), data.ToList());
    }
}