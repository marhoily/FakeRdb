namespace FakeRdb;

public static class SelectExecutor
{
    public static QueryResult Select(Table[] tables, IR.ResultColumn[] projection, IR.IExpression? where, IR.OrderingTerm[] ordering)
    {
        var table = tables.Single();
        var proj = projection.Select(c => c.Exp).ToArray();
        var data = BuildData(table, proj, where, ordering);
        var schema = BuildSchema(projection, proj);
        return new QueryResult(schema, data);

        List<List<object?>> BuildData(Table source,
            IR.IExpression[] selectors, IR.IExpression? filter,
            IR.OrderingTerm[] orderingTerms)
        {
            var temp = ApplyFilter(source, filter).ToList();

            // We cannot take sorting out of here to the later stages
            // because the projection can throw the sorted columns a away
            ApplyOrdering(temp, orderingTerms);

            return ApplyProjection(temp, selectors);
        }

        ResultSchema BuildSchema(IR.ResultColumn[] columns, IR.IExpression[] expressions)
        {
            return new ResultSchema(columns.Zip(expressions)
                .Select(column => new ColumnDefinition(
                    column.First.Alias ??
                    AsColumn(column.First.Exp)?.Name ??
                    column.First.Original,
                    AsColumn(column.First.Exp)?.ColumnType ??
                    TypeAffinity.NotSet))
                .ToArray());

            Column? AsColumn(IR.IExpression exp) =>
                exp is IR.ColumnExp col ? col.Value : null;
        }

        IEnumerable<Row> ApplyFilter(Table source, IR.IExpression? expression)
        {
            return expression == null
                ? source
                : source.Where(expression.Eval<bool>);
        }

        List<List<object?>> ApplyProjection(IEnumerable<Row> rows, IR.IExpression[] selectors)
        {
            return rows
                .Select(row => selectors
                    .Select(selector => selector.Eval(row))
                    .ToList())
                .ToList();
        }

        static void ApplyOrdering(List<Row> temp, IR.OrderingTerm[] orderingTerms)
        {
            foreach (var orderingTerm in orderingTerms)
                temp.Sort(Row.Comparer(
                    orderingTerm.Column.ColumnIndex));
        }
    }

}