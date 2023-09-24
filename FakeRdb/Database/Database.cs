using static FakeRdb.IR;

namespace FakeRdb;

public sealed class Database : Dictionary<string, Table>
{
    public QueryResult Select(Table table, ResultColumn[] projection, IExpression? where, OrderingTerm[] ordering)
    {
        var proj = projection.Select(c => c.Exp).ToArray();
        var data = BuildData(table, proj, where, ordering);
        var schema = BuildSchema(projection, proj);
        return new QueryResult(schema, data);

        List<List<object?>> BuildData(Table source,
            IExpression[] selectors, IExpression? filter,
            OrderingTerm[] orderingTerms)
        {
            var temp = ApplyFilter(source, filter).ToList();

            // We cannot take sorting out of here to the later stages
            // because the projection can throw the sorted columns a away
            ApplyOrdering(temp, orderingTerms);

            return ApplyProjection(temp, selectors);
        }

        ResultSchema BuildSchema(ResultColumn[] columns, IExpression[] expressions)
        {
            return new ResultSchema(columns.Zip(expressions)
                .Select(column => new ColumnDefinition(
                    column.First.Alias ??
                    AsColumn(column.First.Exp)?.Name ??
                    column.First.Original,
                    AsColumn(column.First.Exp)?.ColumnType ??
                    TypeAffinity.NotSet))
                .ToArray());

            Column? AsColumn(IExpression exp) =>
                exp is ColumnExp col ? col.Value : null;
        }

        IEnumerable<Row> ApplyFilter(Table source, IExpression? expression)
        {
            return expression == null
                ? source
                : source.Where(expression.Eval<bool>);
        }

        List<List<object?>> ApplyProjection(IEnumerable<Row> rows, IExpression[] selectors)
        {
            return rows
                .Select(row => selectors
                    .Select(selector => selector.Eval(row))
                    .ToList())
                .ToList();
        }

        static void ApplyOrdering(List<Row> temp, OrderingTerm[] orderingTerms)
        {
            foreach (var orderingTerm in orderingTerms)
                temp.Sort(Row.Comparer(
                    orderingTerm.Column.ColumnIndex));
        }
    }

    public QueryResult SelectAggregate(Table table, ResultColumn[] projection, Column[] groupBy)
    {
        var aggregate = projection
            .Where(col => col.Exp is AggregateExp)
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
        int GetIndex(ResultColumn column) =>
            column.Exp switch
            {
                AggregateExp => aggregateColumnIndex++,
                ColumnExp n => n.Value.ColumnIndex,
                _ => throw new NotImplementedException()
            };
        // contains indices into the 'data' table
        var projectionKeys = projection.Select(GetIndex).ToArray();
        var resultRows = data.Select(record =>
            projectionKeys.Select(k => record[k]).ToList())
            .ToList();

        // ---------- build schema -----------
        ColumnDefinition OneColumn(ResultColumn resultColumn, object? firstValue) =>
            resultColumn.Exp switch
            {
                AggregateExp => new ColumnDefinition(
                    resultColumn.Alias ?? resultColumn.Original,
                    firstValue.CalculateEffectiveAffinity()),
                ColumnExp n => new ColumnDefinition(n.Value.Name, n.Value.ColumnType),
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

    public void Insert(string tableName, string[] columns, ValuesTable values)
    {
        var table = this[tableName];
        if (columns.Length == 0) // TODO: Should it be here or in outer method? Is there more efficient way?
            columns = table.Schema.Columns.Select(c => c.Name).ToArray();

        var columnGenerator = table.Schema.Columns
            .Select(PrepareColumnValueGenerator)
            .ToArray();

        for (var i = 0; i < values.Rows.Length; i++)
            table.Add(columnGenerator.Select(gen => gen(i)).ToArray());

        return;

        Func<int, object?> PrepareColumnValueGenerator(Column column)
        {
            var col = Array.IndexOf(columns, column.Name);
            if (col != -1)
                return row =>
                    values.Rows[row].Cells[col]
                        .Eval()
                        .ConvertToSqliteType(column.ColumnType);

            if (column.IsAutoincrement)
                return _ => table.Autoincrement();

            return _ => column.ColumnType switch
            {
                TypeAffinity.Numeric => 0,
                TypeAffinity.Integer => 0,
                TypeAffinity.Real => 0.0,
                TypeAffinity.Text => "",
                TypeAffinity.Blob => Array.Empty<byte>(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    public int Update(
        string tableName,
        (string column, IExpression value)[] assignments,
        IExpression? filter)
    {
        var table = this[tableName] ?? throw new ArgumentOutOfRangeException(nameof(tableName));
        var schema = table.Schema;
        var compiled = assignments.Select(x =>
                (column: schema.IndexOf(x.column), x.value))
            .ToArray();
        var counter = 0;
        foreach (var row in table)
            if (filter == null || filter.Eval<bool>(row))
            {
                counter++;
                foreach (var (column, value) in compiled)
                {
                    row.Data[column] = value.Eval(row)
                        .Coerce(schema.Columns[column].ColumnType);
                }
            }
        return counter;
    }
    public int Delete(string tableName, IExpression? projection)
    {
        var table = this[tableName];
        if (projection != null)
            return table.RemoveAll(projection.Eval<bool>);

        var affected = table.Count;
        table.Clear();
        return affected;
    }

    public Table? Try(string? tableName)
    {
        return tableName == null ? null : this[tableName];
    }
    public Table[]? TrySet(string? tableName)
    {
        return tableName == null ? null : new[] { this[tableName] };
    }
}