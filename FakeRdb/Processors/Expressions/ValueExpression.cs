namespace FakeRdb;

public sealed class ValueExpression : IExpression
{
    private readonly object? _value;
    public ValueExpression(object? value, SqliteTypeAffinity type, string exp)
    {
        _value = value.Coerce(type);
        ExpressionType = type;
        ResultSetName = exp;
    }

    public SqliteTypeAffinity ExpressionType { get; }
    public string ResultSetName { get; }

    public object? Eval() => _value;
    public object? Eval(Row dataSet) => _value;
    public object? Eval(params Row[] dataSet) => _value;
}