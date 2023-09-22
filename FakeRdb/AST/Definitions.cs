namespace FakeRdb;

/// <summary> Intermediate representation </summary>
public interface IR
{
    public interface IResultColumn : IR { }
    public interface IExpression : IR { }

    public sealed record SelectStmt(string TableName, SelectCore[] Queries, OrderingTerm[] OrderingTerms) : IR;
    public sealed record SelectCore(string TableName, IResultColumn[] Columns) : IR;
    public sealed record OrderingTerm(IExpression Exp) : IR;

    public sealed record Wildcard : IResultColumn;
    public sealed record ResultColumn(IExpression Exp, string? Alias) : IResultColumn;

    public sealed record BindExp(string ParameterName) : IExpression;
    public sealed record BinaryExp(IExpression Left, IExpression Right) : IExpression;
    public sealed record CallExp(string FunctionName, IExpression Arg) : IExpression;
    public sealed record ColumnExp(string TableName, string ColumnName) : IExpression;
    public sealed record LiteralExp(string Value) : IExpression;
}