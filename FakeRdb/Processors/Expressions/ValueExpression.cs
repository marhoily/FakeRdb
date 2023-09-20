namespace FakeRdb;

public sealed class ValueExpression : Expression
{
    private object? _value;
    public ValueExpression(object? value, SqliteTypeAffinity type, string exp)
    {
        _value = value.Coerce(type);
        ExpressionType = type;
        ResultSetName = exp;
    }

    public override SqliteTypeAffinity ExpressionType { get; }
    public override string ResultSetName { get; }

    protected override void SetTarget(Field field) =>
        _value = _value.Coerce(field.FieldType);

    public override object? Resolve(params Row[] dataSet) => _value;
}