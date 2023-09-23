namespace FakeRdb;

public delegate AggregateResult AggregateFunction(Row[] dataSet, IR.IExpression[] args);
public delegate string ScalarFunction(Row row, IR.IExpression[] args);

/// <summary> Intermediate representation </summary>
public interface IR : IResult
{
    public interface IExpression : IR { }
    public interface ICompoundSelect : IR { }

    public sealed record SelectStmt(ICompoundSelect Query, OrderingTerm[] OrderingTerms) : IR;
    public sealed record CompoundSelect(CompoundOperator Operator, ICompoundSelect Left, ICompoundSelect Right) : ICompoundSelect;
    public sealed record SelectCore(Table From, ResultColumn[] Columns, IExpression? Where) : ICompoundSelect;
    public sealed record OrderBy(OrderingTerm[] Terms) : IR;
    public sealed record OrderingTerm(Field Column) : IR;

    public sealed record ResultColumnList(params ResultColumn[] List) : IR;
    public sealed record ResultColumn(IExpression Exp, string Original, string? Alias = null) : IR;

    public sealed record BindExp(object? Value) : IExpression;
    public sealed record BinaryExp(BinaryOperator Op, IExpression Left, IExpression Right) : IExpression;
    public sealed record AggregateExp(AggregateFunction Function, IExpression[] Args) : IExpression;
    public sealed record ScalarExp(ScalarFunction Function, IExpression[] Args) : IExpression;
    public sealed record ColumnExp(Field Value) : IExpression;
    public sealed record LiteralExp(string Value) : IExpression;
    public sealed record InExp(IExpression Needle, QueryResult Haystack) : IExpression;

    public sealed record ValuesTable(ValuesRow[] Rows) : IResult;
    public sealed record ValuesRow(IExpression[] Cells);
    public static QueryResult Execute(SelectStmt stmt)
    {
        return Recursive(stmt.Query, stmt.OrderingTerms);
        static QueryResult Union(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
    
            // Use Distinct to remove duplicates. This assumes that List<object?> implements appropriate equality semantics.
            var resultData = x.Data.Concat(y.Data)
                .Distinct(new RowEqualityComparer<object?>())
                .Order(new SelectiveComparer(0))
                .ToList();
    
            return new QueryResult(x.Schema, resultData);
        }


        static QueryResult Intersect(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
            var customComparer = new RowEqualityComparer<object?>();
            var resultData = x.Data
                .Intersect(y.Data, customComparer)
                .Order(new SelectiveComparer(0))
                .ToList();
            return new QueryResult(x.Schema, resultData);
        }

        static QueryResult Except(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
    
            var customComparer = new RowEqualityComparer<object?>();
            var resultData = x.Data
                .Except(y.Data, customComparer)
                .Order(new SelectiveComparer(0))
                .ToList();
    
            return new QueryResult(x.Schema, resultData);
        }
        static QueryResult UnionAll(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
            var resultData = new List<List<object?>>(x.Data);
            resultData.AddRange(y.Data);
            return new QueryResult(x.Schema, resultData);
        }

        static void ValidateSchema(ResultSchema x, ResultSchema y)
        {
            if (x.Columns.Length != y.Columns.Length)
            {
                throw new ArgumentException("SELECTs to the left and right of UNION do not have the same number of result columns");
            }
        }

        static QueryResult Recursive(ICompoundSelect query, params OrderingTerm[] orderingTerms)
        {
            if (query is SelectCore core)
                return Terminal(core, orderingTerms);
            if (query is CompoundSelect compound)
            {
                var left = Recursive(compound.Left);
                var right = Recursive(compound.Right);
                return compound.Operator switch
                {
                    CompoundOperator.Union => Union(left, right),
                    CompoundOperator.UnionAll => UnionAll(left, right),
                    CompoundOperator.Intersect => Intersect(left, right),
                    CompoundOperator.Except => Except(left, right),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            throw new ArgumentOutOfRangeException();
        }
        static QueryResult Terminal(SelectCore query, params OrderingTerm[] stmtOrderingTerms)
        {
            var orderingTerm = stmtOrderingTerms.FirstOrDefault();
            var aggregate = query.Columns
                .Where(c => c.Exp is AggregateExp)
                .ToList();
            if (aggregate.Count > 0)
            {
                return query.From.SelectAggregate(aggregate);
            }

            var result = query.From.Select(query.Columns, query.Where);
            if (orderingTerm != null)
            {
                var clause = new OrderByClause(orderingTerm.Column);
                result.Data.Sort(clause.GetComparer(result.Schema));
            }
            return result;

        }
    }
}