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

    public QueryResult SelectAggregate(List<ResultColumn> aggregate, Column[] queryGroupBy)
    {
        var rows = ToArray();
      //  rows.GroupBy()
        var schema = new List<ColumnDefinition>();
        var data = new List<object?>();
        foreach (var resultColumn in aggregate)
        {
            var cell = resultColumn.Exp
                .Eval<AggregateResult>(rows);
            schema.Add(new ColumnDefinition(resultColumn.Original,
                cell.Value.GetSimplifyingAffinity()));
            data.Add(cell.Value);
        }

        return new QueryResult(
            new ResultSchema(schema.ToArray()),
            new List<List<object?>>
            {
                data
            });
    }

}