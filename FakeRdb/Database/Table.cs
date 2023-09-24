using static FakeRdb.IR;

namespace FakeRdb;

public sealed class Table : List<Row>
{
    public TableSchema Schema { get; }
    private int _autoincrement;

    public Table(TableSchema schema) => Schema = schema;
    public long Autoincrement() => ++_autoincrement;
    public void Add(object?[] oneRow) => Add(new Row(oneRow));


    public QueryResult Select(ResultColumn[] projection, IExpression? where, OrderingTerm[] ordering)
    {
        var proj = projection.Select(c => c.Exp).ToArray();
        var data = BuildData(this, proj, where, ordering);
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

    public QueryResult SelectAggregate(ResultColumn[] projection, Column[] groupBy)
    {
        var prefix = Schema.Columns.Length;
        var aggregate = projection
            .Where(col => col.Exp is AggregateExp)
            .ToArray();
        var keyIndices = groupBy.Select(g => g.ColumnIndex).ToList();

        // ------ retrieve --------
        static IEnumerable<object?> Combine(AggregateResult[] src)
        {
            var plainColumns = src.First().Row;
            var aggregatedColumns = src.Select(x => x.Value).ToArray();
            return plainColumns.Concat(aggregatedColumns);
        }

        var data = this
            .GroupBy(row => row.GetKey(keyIndices))
            .OrderBy(g => g.Key)
            .Select(group => Combine(aggregate
                .Select(col => col.Exp.Eval<AggregateResult>(group.ToArray()))
                .ToArray())
                .ToList())
            .ToList();

        // ---------- project -----------
        var aggregateCount = 0;
        int GetIndex(ResultColumn column) =>
            column.Exp switch
            {
                AggregateExp => prefix + (aggregateCount++),
                ColumnExp n => n.Value.ColumnIndex,
                _ => throw new ArgumentOutOfRangeException()
            };

        var projectionKeys = projection.Select(GetIndex).ToArray();
        var result = data.Select(record => 
            projectionKeys.Select(k => record[k]).ToList())
            .ToList();

        // ---------- build schema -----------
        ColumnDefinition OneColumn(ResultColumn resultColumn, object? firstValue) =>
            resultColumn.Exp switch
            {
                AggregateExp  =>  new ColumnDefinition(
                    resultColumn.Alias ??resultColumn.Original,
                    firstValue.GetSimplifyingAffinity()),
                ColumnExp n => new ColumnDefinition(n.Value.Name, n.Value.ColumnType),
                _ => throw new ArgumentOutOfRangeException()
            };

        var schema = projection
            .Zip(result.First())
            .Select(pair => OneColumn(pair.First, pair.Second))
            .ToArray();

        return new QueryResult(new ResultSchema(schema), result);
    }
}