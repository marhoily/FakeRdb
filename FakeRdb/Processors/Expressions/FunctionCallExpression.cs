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

    public override object Resolve(params Row[] dataSet) =>
        _functionName.ToUpperInvariant() switch
        {
            "MAX" => Max(dataSet),
            "MIN" => Min(dataSet),
            _ => throw new ArgumentOutOfRangeException("Unknown:" + _functionName)
        };

    private AggregateResult Max(Row[] dataSet)
    {
        var expression = _args.Single();
        var row = dataSet.MaxBy(r => expression.Resolve(r)) ??
                  throw new NotImplementedException();
        return new AggregateResult(row, expression.Resolve(row));
    }
    private AggregateResult Min(Row[] dataSet)
    {
        var expression = _args.Single();
        var row = dataSet.MinBy(r => expression.Resolve(r)) ??
                  throw new NotImplementedException();
        return new AggregateResult(row, expression.Resolve(row));
    }

    public override DynamicType ExpressionType => _args[0].ExpressionType;

    public override string ResultSetName =>
        $"{_functionName}({string.Join(", ", _args.Select(a => a.ResultSetName))})";
}