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
        var data = BuildData(this, proj, where);
        var schema = BuildSchema(projection, proj);
        ApplyOrdering(data, schema, ordering);
        return new QueryResult(schema, data);

        List<List<object?>> BuildData(Table source,
            IExpression[] selectors, IExpression? filter)
        {
            var temp = ApplyFilter(source, filter);
            return ApplyProjection(temp, selectors);
        }

        ResultSchema BuildSchema(ResultColumn[] columns, IExpression[] expressions)
        {
            return new ResultSchema(columns.Zip(expressions)
                .Select(column => new ColumnDefinition(
                    column.First.Alias ??
                    ExtractColumnName(column.First.Exp) ??
                    column.First.Original,
                    ExtractColumnType(column.First.Exp) ??
                    TypeAffinity.NotSet))
                .ToArray());
            string? ExtractColumnName(IExpression exp)
            {
                return exp is ColumnExp col ? col.Value.Name : null;
            }
            TypeAffinity? ExtractColumnType(IExpression exp)
            {
                return exp is ColumnExp col ? col.Value.ColumnType : null;
            }
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

        static void ApplyOrdering(List<List<object?>> list, ResultSchema resultSchema, OrderingTerm[] orderingTerms)
        {
            foreach (var orderingTerm in orderingTerms)
                list.Sort(new RowByColumnComparer(
                    resultSchema.IndexOf(orderingTerm.Column)));
        }
    }

    public QueryResult SelectAggregate(List<ResultColumn> aggregate)
    {
        var rows = ToArray();
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