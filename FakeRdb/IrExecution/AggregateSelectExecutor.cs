using static FakeRdb.IR;

namespace FakeRdb;

public static class AggregateSelectExecutor
{
    public static Table SelectAggregate(Table table, 
        ResultColumn[] projection, ColumnHeader[] groupBy)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return table.GroupBy(
            groupBy.Select(c => table.Columns[c.ColumnIndex]).ToArray(),
            projection);
    }
}