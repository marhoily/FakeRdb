namespace FakeRdb;

/// <summary> Intermediate representation </summary>
public interface IR : IResult
{
    public interface IExpression : IR { }

    public sealed record SelectStmt(SelectCore[] Queries, OrderingTerm[] OrderingTerms) : IR;
    public sealed record SelectCore(string TableName, ResultColumn[] Columns, IExpression? Where) : IR;
    public sealed record OrderBy(OrderingTerm[] Terms) : IR;
    public sealed record OrderingTerm(Field Column) : IR;

    public sealed record ResultColumnList(params ResultColumn[] List) : IR;
    public sealed record ResultColumn(IExpression Exp, string? Alias = null) : IR;

    public sealed record BindExp(string ParameterName, object? Value) : IExpression;
    public sealed record BinaryExp(Operator Op, IExpression Left, IExpression Right, string Alias) : IExpression;
    public sealed record CallExp(string FunctionName, IExpression[] Args) : IExpression;
    public sealed record ColumnExp(Field Value) : IExpression;
    public sealed record LiteralExp(string Value) : IExpression;
    public sealed record InExp(IExpression Needle, SelectCore Haystack) : IExpression;

    public static QueryResult Execute(Database db, SelectStmt stmt)
    {
        return Inner(stmt.Queries.Single(),
            stmt.OrderingTerms.FirstOrDefault());
        QueryResult Inner(SelectCore query, OrderingTerm? orderingTerm)
        {
            var expressions = query.Columns
                .Select(c => c.Exp.Convert())
                .ToArray();
            var aggregate = expressions
                .OfType<FunctionCallExpression>()
                .Where(f => f.IsAggregate)
                .ToList();
            if (aggregate.Count > 0)
                return db.SelectAggregate(query.TableName, aggregate);
            var projection = expressions.Cast<IProjection>().ToArray();
            var result = db.Select2(query.TableName, projection, query.Where?.Convert());
            if (orderingTerm != null)
            {
                result.Data.Sort(new OrderByClause(orderingTerm.Column)
                    .GetComparer(result.Schema));
            }
            return result;

        }
    }
}
public static class X
{
    public static IExpression Convert(this IR.IExpression arg)
    {
        return arg switch
        {
            IR.BinaryExp binaryExp => new BinaryExpression(binaryExp.Op, Convert(binaryExp.Left), Convert(binaryExp.Right), binaryExp.Alias),
            IR.BindExp bindExp => new ValueExpression(bindExp.Value, bindExp.Value.GetTypeAffinity(), bindExp.ParameterName),
            IR.CallExp callExp => new FunctionCallExpression(callExp.FunctionName, callExp.Args.Select(Convert).ToArray()),
            IR.ColumnExp columnExp => new ProjectionExpression(columnExp.Value),
            IR.InExp inExp => new InExpression(inExp.Needle.Convert(), null!),
            IR.LiteralExp literalExp => new ValueExpression(literalExp.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }

    public static QueryResult Select2(this Database db, string tableName, IProjection[] projection, IExpression? filter)
    {
        var table = db[tableName];
        var selectors = projection.OfType<IExpression>().ToArray();
        var data = BuildData(table, selectors, filter);
        var schema = BuildSchema(selectors);
        return new QueryResult(schema, data);

        static List<List<object?>> BuildData(Table source, IExpression[] proj, IExpression? filter)
        {
            var temp = ApplyFilter(source, filter);
            return ApplyProjection(temp, proj);
        }

        static ResultSchema BuildSchema(IEnumerable<IExpression> selectors)
        {
            return new ResultSchema(selectors
                .Select(column => new ColumnDefinition(
                    column.ResultName,
                    column.ExpressionType))
                .ToArray());
        }

        static IEnumerable<Row> ApplyFilter(Table source, IExpression? expression)
        {
            return expression == null
                ? source
                : source.Where(expression.Eval<bool>);
        }

        static List<List<object?>> ApplyProjection(IEnumerable<Row> rows, IExpression[] selectors)
        {
            return rows
                .Select(row => selectors
                    .Select(selector => selector.Eval(row))
                    .ToList())
                .ToList();
        }
    }

}