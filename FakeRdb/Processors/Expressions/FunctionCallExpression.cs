namespace FakeRdb;

public sealed class FunctionCallExpression : Expression
{
    private readonly string _functionName;
    private readonly Expression[] _args;

    public FunctionCallExpression(string functionName, Expression[] args)
    {
        _functionName = functionName;
        _args = args;
    }

    protected override void SetTarget(Field field)
    {
        throw new NotImplementedException();
    }

    protected override void SetValue(object value)
    {
        throw new NotImplementedException();
    }

    public override object? Resolve(Row row)
    {
        throw new NotImplementedException();
    }

    public override Type ExpressionType => _functionName switch
    {
        "Max" => _args.Single().ExpressionType,
        _ => throw new ArgumentOutOfRangeException("Unknown:" + _functionName)
    };
}