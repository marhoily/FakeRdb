namespace FakeRdb;

public sealed class FieldAccessExpression : Expression
{
    public Field AccessedField { get; }
    public FieldAccessExpression(Field field) => AccessedField = field;
    /*
     * When an expression is a simple reference to a column of a real
     * table (not a VIEW or subquery) then the expression has the same
     * affinity as the table column. 
     */
    public override SqliteTypeAffinity ExpressionType => AccessedField.FieldType;
    public override string ResultSetName => AccessedField.Name;


    public override object? Resolve(params Row[] dataSet)
    {
        if (AccessedField == null)
            throw new InvalidOperationException(
                "Cannot resolve value without column");
        return dataSet[0][AccessedField].Coerce(AccessedField.FieldType);
    }

}