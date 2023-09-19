namespace FakeRdb;

public sealed class FieldAccessExpression : Expression
{
    private readonly Field _accessedField;
    public FieldAccessExpression(Field field) => _accessedField = field;

    protected override void SetTarget(Field targetField)
    {
        // Even if we are trying to access X when assigning Y,
        // there's nothing to do here
    }


    protected override void SetValue(object value) => throw new NotSupportedException();

    public override object? Resolve(Row row)
    {
        if (_accessedField == null)
            throw new InvalidOperationException(
                "Cannot resolve value without column");
        return Convert.ChangeType(row[_accessedField], _accessedField.FieldType);
    }

}