namespace FakeRdb;

public sealed class ValueExpression : IExpression
{
    private readonly object? _value;
    public ValueExpression(object? value, SqliteTypeAffinity type, string exp)
    {
        _value = value.Coerce(type);
        ExpressionType = type;
        ResultName = exp;
    }

    public SqliteTypeAffinity ExpressionType { get; }
    public string ResultName { get; private set; }
    public void SetAlias(string value) => ResultName = value;

    public object? Eval() => _value;
    public object? Eval(Row dataSet) => _value;
    public object? Eval(Row[] dataSet) => _value;
}