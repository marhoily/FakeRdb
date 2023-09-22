namespace FakeRdb;

public delegate AggregateResult AggregateFunction(Row[] dataSet, IExpression[] args);
public delegate string ScalarFunction(Row row, IExpression[] args);

/// <summary> Intermediate representation </summary>
public interface IR : IResult
{
    public interface IExpression : IR { }

    public sealed record SelectStmt(SelectCore[] Queries, OrderingTerm[] OrderingTerms) : IR;
    public sealed record SelectCore(Table From, ResultColumn[] Columns, IExpression? Where) : IR;
    public sealed record OrderBy(OrderingTerm[] Terms) : IR;
    public sealed record OrderingTerm(Field Column) : IR;

    public sealed record ResultColumnList(params ResultColumn[] List) : IR;
    public sealed record ResultColumn(IExpression Exp, string Original, string? Alias = null) : IR;

    public sealed record BindExp(object? Value) : IExpression;
    public sealed record BinaryExp(Operator Op, IExpression Left, IExpression Right) : IExpression;
    public sealed record AggregateExp(AggregateFunction Function, IExpression[] Args) : IExpression;
    public sealed record ScalarExp(ScalarFunction Function, IExpression[] Args) : IExpression;
    public sealed record ColumnExp(Field Value) : IExpression;
    public sealed record LiteralExp(string Value) : IExpression;
    public sealed record InExp(IExpression Needle, QueryResult Haystack) : IExpression;

    public static QueryResult Execute(SelectStmt stmt)
    {
        return Inner(stmt.Queries.Single(),
            stmt.OrderingTerms.FirstOrDefault());
        static QueryResult Inner(SelectCore query, OrderingTerm? orderingTerm)
        {
            var aggregate = query.Columns
                .Where(c => c.Exp is AggregateExp)
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