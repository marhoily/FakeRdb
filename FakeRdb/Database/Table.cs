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

    public QueryResult SelectAggregate(List<ResultColumn> aggregate, Column[] groupBy)
    {
        var keyIndices = groupBy.Select(g => g.ColumnIndex).ToList();

        var data = this
            .GroupBy(row => row.GetKey(keyIndices))
            .Select(group => aggregate
                .Select(col => col.Exp.Eval<AggregateResult>(group.ToArray()))
               // .Select(r => r.Row.Append(r.Value))
                .Select(r => (IEnumerable<object?>)new[]{r.Value})
                .Aggregate((x, y) => x.Append(y))
                .ToList())
            .ToList();

        var schema =/*Schema.Columns.Select(col =>
                new ColumnDefinition(col.Name, col.ColumnType))
            .Concat(*/aggregate.Zip(data.First()/*.Skip(Schema.Columns.Length)*/)
                .Select(col => new ColumnDefinition(col.First.Original,
                    col.Second.GetSimplifyingAffinity()))/*)*/
            .ToArray();

        return new QueryResult(new ResultSchema(schema), data);
    }
}