using static FakeRdb.IR;

namespace FakeRdb;

public static class SelectExecutor
{
    public static QueryResult Select(Table[] tables, ResultColumn[] columns, IExpression? where,
        OrderingTerm[] ordering)
    {
        var table = tables.Single();
        var projection = columns.Select(c => c.Exp).ToArray();
        var data = BuildData(table, projection, where, ordering);
        var schema = BuildSchema(columns, projection);
        return new QueryResult(schema, data);
    }

    private static List<List<object?>> BuildData(Table source,
            IExpression[] selectors, IExpression? filter,
            OrderingTerm[] orderingTerms)
    {
        var temp = ApplyFilter(source, filter).ToList();

        // We cannot take sorting out of here to the later stages
        // because the projection can throw the sorted columns a away
        ApplyOrdering(temp, orderingTerms);

        return ApplyProjection(temp, selectors);
    }

    private static ResultSchema BuildSchema(ResultColumn[] columns, IExpression[] projection)
    {
        return new ResultSchema(columns.Zip(projection)
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

    private static IEnumerable<Row> ApplyFilter(Table source, IExpression? expression)
    {
        return expression == null
            ? source
            : source.Where(expression.Eval<bool>);
    }

    private static List<List<object?>> ApplyProjection(IEnumerable<Row> rows, IExpression[] selectors)
    {
        return rows
            .Select(row => selectors
                .Select(selector => selector.Eval(row))
                .ToList())
            .ToList();
    }

    private static void ApplyOrdering(List<Row> temp, OrderingTerm[] orderingTerms)
    {
        foreach (var orderingTerm in orderingTerms)
            temp.Sort(Row.Comparer(
                orderingTerm.Column.ColumnIndex));
    }


}