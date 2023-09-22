namespace FakeRdb;

public sealed class ValueExpression : IExpression
{
    private readonly object? _value;
    public ValueExpression(object? value, TypeAffinity type)
    {
        _value = value.Coerce(type);
    }
    public ValueExpression(string value)
    {
        _value = value.CoerceToLexicalAffinity();
        _value.GetTypeAffinity();
    }

    public object? Eval() => _value;
    public object? Eval(Row dataSet) => _value;
    public object? Eval(Row[] dataSet) => _value;
}