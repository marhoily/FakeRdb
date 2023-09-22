namespace FakeRdb;

public sealed class ProjectionExpression : IExpression
{
    public Field SelectColumn { get; }

    public ProjectionExpression(Field selectColumn)
    {
        SelectColumn = selectColumn;
    }

    /*
     * When an expression is a simple reference to a column of a real
     * table (not a VIEW or subquery) then the expression has the same
     * affinity as the table column.
     */
    public TypeAffinity ExpressionType => SelectColumn.FieldType;

    public object Eval() => throw new NotSupportedException();
    public object Eval(Row[] dataSet) => throw new NotSupportedException();
    public object? Eval(Row dataSet) => dataSet[SelectColumn];
}