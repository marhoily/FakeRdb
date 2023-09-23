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
}