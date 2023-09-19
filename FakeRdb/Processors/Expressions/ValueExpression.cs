namespace FakeRdb;

public sealed class ValueExpression : Expression
{
    private object? _value;
    public ValueExpression(object? value) => _value = value;
    public override Type ExpressionType => _value?.GetType() ?? typeof(DBNull);

    protected override void SetTarget(Field field) =>
        _value = Convert.ChangeType(_value, field.FieldType);

    protected override void SetValue(object value) =>
        throw new NotSupportedException();

    public override object? Resolve(params Row[] row) => _value;
}