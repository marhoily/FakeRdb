namespace FakeRdb;

/// <summary> Intermediate representation </summary>
public interface IR : IResult
{
    public delegate object? AggregateFunction(Table table, IEnumerable<int> rowSet, IExpression[] args);
    public delegate string ScalarFunction(Table table, int rowIndex, IExpression[] args);

    public interface ICompoundSelect : IR { }

    public sealed record SelectStmt(ICompoundSelect Query, params OrderingTerm[] OrderingTerms) : IR;
    public sealed record CompoundSelect(CompoundOperator Operator, ICompoundSelect Left, ICompoundSelect Right) : ICompoundSelect;
    /// <summary>
    /// Represents the core part of a SELECT query. This class is built from
    /// multiple `DisjunctiveQuery` which collectively serve as the data sources 
    /// for the query. Each "DisjunctiveQuery" instance encapsulates a disjunction
    /// (logical OR) of sets of conditions (alternatives) that together form a valid
    /// 'scenario' or 'case' for fetching records.
    ///
    /// <p>In this way, the "Sources" parameter effectively describes all the different
    /// combinations of conditions that would satisfy the SELECT query, onto which
    /// a projection ("Columns") and grouping ("GroupBy") are applied.</p>
    /// </summary>
    public sealed record SelectCore(
        CompositeCondition[] AlternativeSources,
        ResultColumn[] Columns,
        string[] GroupBy) : ICompoundSelect;
    public sealed record OrderBy(OrderingTerm[] Terms) : IR;
    public sealed record OrderingTerm(string FullColumnName) : IR;

    public sealed record ResultColumnList(params ResultColumn[] List) : IR;
    public sealed record ResultColumn(IExpression Exp, string Original, string? Alias = null) : IR;

    public interface IExpression : IR { }
    public sealed record UnaryExp(UnaryOperator Op, IExpression Operand) : IExpression;
    public sealed record BinaryExp(BinaryOperator Op, IExpression Left, IExpression Right) : IExpression;
    public sealed record ColumnExp(string FullColumnName) : IExpression;
    public sealed record LiteralExp(string Value) : IExpression;

    public sealed record BindExp(object? Value) : IExpression;
    public sealed record AggregateExp(AggregateFunction Function, IExpression[] Args) : IExpression;
    public sealed record ScalarExp(ScalarFunction Function, IExpression[] Args) : IExpression;
    public sealed record InExp(IExpression Needle, Column Haystack) : IExpression;

    public sealed record CompositeCondition(
        SingleTableCondition[] SingleTableConditions,
        EquiJoinCondition[] EquiJoinConditions,
        IExpression? GeneralCondition);

    /// <summary>
    /// Represents conditions specific to a single table.
    /// The Filter should only involve columns from one table and contain no OR/AND.
    /// </summary>
    public sealed record SingleTableCondition(Table Table, IExpression Filter);

    public sealed record EquiJoinCondition(
        string LeftTable, string LeftColumn, 
        string RightTable, string RightColumn);

  
    public sealed record ValuesTable(ValuesRow[] Rows) : IResult;
    public sealed record ValuesRow(IExpression[] Cells);
}