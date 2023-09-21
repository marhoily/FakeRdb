namespace FakeRdb;

public sealed class ValueExpression : IExpression
{
    private readonly object? _value;
    public ValueExpression(object? value, TypeAffinity type, string exp)
    {
        _value = value.Coerce(type);
        ExpressionType = type;
        ResultName = exp;
    }
    public ValueExpression(string value)
    {
        ExpressionType = value.GetLexicalAffinity();
        _value = value.CoerceToLexicalAffinity();
        ResultName = value;
    }

    public TypeAffinity ExpressionType { get; }
    public string ResultName { get; private set; }
    public void SetAlias(string value) => ResultName = value;

    public object? Eval() => _value;
    public object? Eval(Row dataSet) => _value;
    public object? Eval(Row[] dataSet) => _value;
}