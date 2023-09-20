namespace FakeRdb;

public sealed class ValueExpression : Expression
{
    private readonly object? _value;
    public ValueExpression(object? value, SqliteTypeAffinity type, string exp)
    {
        _value = value.Coerce(type);
        ExpressionType = type;
        ResultSetName = exp;
    }

    public override SqliteTypeAffinity ExpressionType { get; }
    public override string ResultSetName { get; }

    public override object? Resolve(params Row[] dataSet) => _value;
}