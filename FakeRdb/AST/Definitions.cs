namespace FakeRdb;

/// <summary> Intermediate representation </summary>
public interface IR : IResult
{
    public interface IExpression : IR { }

    public sealed record SelectStmt(SelectCore[] Queries, OrderingTerm[] OrderingTerms) : IR;
    public sealed record SelectCore(Table From, ResultColumn[] Columns, IExpression? Where) : IR;
    public sealed record OrderBy(OrderingTerm[] Terms) : IR;
    public sealed record OrderingTerm(Field Column) : IR;

    public sealed record ResultColumnList(params ResultColumn[] List) : IR;
    public sealed record ResultColumn(IExpression Exp, string? Alias = null) : IR;

    public sealed record BindExp(string ParameterName, object? Value) : IExpression;
    public sealed record BinaryExp(Operator Op, IExpression Left, IExpression Right, string Alias) : IExpression;
    public sealed record CallExp(string FunctionName, IExpression[] Args) : IExpression;
    public sealed record ColumnExp(Field Value) : IExpression;
    public sealed record LiteralExp(string Value) : IExpression;
    public sealed record InExp(IExpression Needle, QueryResult Haystack) : IExpression;

    public static QueryResult Execute(SelectStmt stmt)
    {
        return Inner(stmt.Queries.Single(),
            stmt.OrderingTerms.FirstOrDefault());
        QueryResult Inner(SelectCore query, OrderingTerm? orderingTerm)
        {
            var aggregate = query.Columns
                .Select(c => c.Exp.Convert())
                .OfType<FunctionCallExpression>()
                .Where(f => f.IsAggregate)
                .ToList();
            if (aggregate.Count > 0)
            {
                return query.From.SelectAggregate(aggregate);
            }

            var result = query.From.Select(query.Columns, query.Where?.Convert());
            if (orderingTerm != null)
            {
                var clause = new OrderByClause(orderingTerm.Column);
                result.Data.Sort(clause.GetComparer(result.Schema));
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
            IR.InExp inExp => new InExpression(inExp.Needle.Convert(), inExp.Haystack),
            IR.LiteralExp literalExp => new ValueExpression(literalExp.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }


}