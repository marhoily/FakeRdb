namespace FakeRdb;

public sealed class ProjectionExpression : IExpression
{
    public Field SelectColumn { get; }

    public ProjectionExpression(Field selectColumn)
    {
        SelectColumn = selectColumn;
        ResultName = SelectColumn.Name;
    }

    /*
     * When an expression is a simple reference to a column of a real
     * table (not a VIEW or subquery) then the expression has the same
     * affinity as the table column.
     */
    public TypeAffinity ExpressionType => SelectColumn.FieldType;
    public string ResultName { get; private set; }
    public void SetAlias(string value) => ResultName = value;

    public object Eval() => throw new NotSupportedException();
    public object Eval(Row[] dataSet) => throw new NotSupportedException();
    public object? Eval(Row dataSet) => dataSet[SelectColumn];
}