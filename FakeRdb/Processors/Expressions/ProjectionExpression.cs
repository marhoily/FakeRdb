namespace FakeRdb;

public sealed class ProjectionExpression : IExpression
{
    public Field AccessedField { get; }
    public ProjectionExpression(Field field) => AccessedField = field;
    /*
     * When an expression is a simple reference to a column of a real
     * table (not a VIEW or subquery) then the expression has the same
     * affinity as the table column. 
     */
    public SqliteTypeAffinity ExpressionType => AccessedField.FieldType;
    public string ResultSetName => AccessedField.Name;



    public object Eval() => throw new NotSupportedException();
    public object Eval(Row[] dataSet) => throw new NotSupportedException();
    public object? Eval(Row dataSet)
    {
        if (AccessedField == null)
            throw new InvalidOperationException(
                "Cannot resolve value without column");
        return dataSet[AccessedField].Coerce(AccessedField.FieldType);
    }

}