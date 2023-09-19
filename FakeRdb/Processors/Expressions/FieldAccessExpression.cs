namespace FakeRdb;

public sealed class FieldAccessExpression : Expression, IProjection
{
    public Field AccessedField { get; }
    public FieldAccessExpression(Field field) => AccessedField = field;
    public override Type ExpressionType => AccessedField.FieldType;

    protected override void SetTarget(Field targetField)
    {
        // Even if we are trying to access X when assigning Y,
        // there's nothing to do here
    }

    protected override void SetValue(object value) => throw new NotSupportedException();

    public override object? Resolve(Row row)
    {
        if (AccessedField == null)
            throw new InvalidOperationException(
                "Cannot resolve value without column");
        return Convert.ChangeType(row[AccessedField], AccessedField.FieldType);
    }
}