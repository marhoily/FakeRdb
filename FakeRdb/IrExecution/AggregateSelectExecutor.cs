namespace FakeRdb;

public static class AggregateSelectExecutor
{
    public static QueryResult SelectAggregate(Table table, IR.ResultColumn[] projection, Column[] groupBy)
    {
        var aggregate = projection
            .Where(col => col.Exp is IR.AggregateExp)
            .ToArray();
        // Flat list of indices of columns that are used for grouping
        var groupByColumnIndices = groupBy
            .Select(g => g.ColumnIndex).ToList();

        // ------ retrieve --------
        static IEnumerable<object?> Combine(AggregateResult[] src)
        {
            var plainColumns = src.First().Row;
            var aggregatedColumns = src.Select(x => x.Value).ToArray();
            return plainColumns.Concat(aggregatedColumns);
        }

        // The 'data' variable holds a 2D table with two distinct sections:
        // 1. The original columns from the source table (left side).
        // 2. One column per aggregate function (right side).
        // For min/max aggregate functions, the rows in the original columns correspond
        // to the row containing the min/max value. For other aggregates, it's from a random row.
        var data = table
            .GroupBy(row => row.GetKey(groupByColumnIndices))
            .OrderBy(g => g.Key) // Required to mimic SQLite's grouping behavior
            .Select(group => Combine(aggregate
                    .Select(col => col.Exp.Eval<AggregateResult>(group.ToArray()))
                    .ToArray())
                .ToList())
            .ToList();

        // ---------- project -----------
        // Calculate starting index for aggregate columns.
        // They are placed after all non-aggregate columns.
        var aggregateColumnIndex = table.Schema.Columns.Length;
        int GetIndex(IR.ResultColumn column) =>
            column.Exp switch
            {
                IR.AggregateExp => aggregateColumnIndex++,
                IR.ColumnExp n => n.Value.ColumnIndex,
                _ => throw new NotImplementedException()
            };
        // contains indices into the 'data' table
        var projectionKeys = projection.Select(GetIndex).ToArray();
        var resultRows = data.Select(record =>
                projectionKeys.Select(k => record[k]).ToList())
            .ToList();

        // ---------- build schema -----------
        ColumnDefinition OneColumn(IR.ResultColumn resultColumn, object? firstValue) =>
            resultColumn.Exp switch
            {
                IR.AggregateExp => new ColumnDefinition(
                    resultColumn.Alias ?? resultColumn.Original,
                    firstValue.CalculateEffectiveAffinity()),
                IR.ColumnExp n => new ColumnDefinition(n.Value.Name, n.Value.ColumnType),
                _ => throw new ArgumentOutOfRangeException()
            };

        // Most operations make sqlite loose the affinity of the original column.
        // In that case, to determine the type of the result column
        // effective affinity of the first-row value is used.
        var schema = projection
            .Zip(resultRows.First())
            .Select(pair => OneColumn(pair.First, pair.Second))
            .ToArray();

        return new QueryResult(new ResultSchema(schema), resultRows);
    }

}