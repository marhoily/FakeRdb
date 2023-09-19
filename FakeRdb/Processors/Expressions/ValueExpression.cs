namespace FakeRdb;

public sealed class ValueExpression : Expression
{
    private object? _value;
    public ValueExpression(object? value, DynamicType type)
    {
        _value = value;
        ExpressionType = type;
    }

    public override DynamicType ExpressionType { get; }
    public override string ResultSetName => "VAL";

    protected override void SetTarget(Field field) =>
        _value = Convert.ChangeType(_value, field.FieldType);

    protected override void SetValue(object value) =>
        throw new NotSupportedException();

    public override object? Resolve(params Row[] dataSet) => _value;
}