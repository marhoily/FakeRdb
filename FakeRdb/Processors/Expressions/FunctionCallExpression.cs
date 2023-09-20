namespace FakeRdb;

public sealed class FunctionCallExpression : Expression, IProjection
{
    private static readonly string[] AggregateFunctions =
    {
        "MAX", "MIN", "AVG"
    };
    private readonly string _functionName;
    private readonly Expression[] _args;

    public bool IsAggregate => AggregateFunctions.Contains(_functionName.ToUpperInvariant());

    public FunctionCallExpression(string functionName, Expression[] args)
    {
        _functionName = functionName;
        _args = args;
    }

    protected override void SetTarget(Field field)
    {
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
            "TYPEOF" => TypeOf(dataSet),
            _ => throw new ArgumentOutOfRangeException("Unknown:" + _functionName)
        };

    private object TypeOf(Row[] dataSet)
    {
        var exp = (FieldAccessExpression)_args.Single();
        var resolve = exp.Resolve(dataSet);
        var field = exp.AccessedField;
        var affinity = field.FieldType.TypeAffinity;
        var result = resolve.GetStorageType(affinity);
        return result.ToString().ToLowerInvariant();
    }

    private AggregateResult Max(Row[] dataSet)
    {
        var expression = _args.Single();
        var row = dataSet.MaxBy(r => expression.Resolve(r), TypeExt.Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row, expression.Resolve(row));
    }
    private AggregateResult Min(Row[] dataSet)
    {
        var expression = _args.Single();
        var row = dataSet.MinBy(r => expression.Resolve(r), TypeExt.Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row, expression.Resolve(row));
    }

    /*
     * An expression of the form "CAST(expr AS type)" has an affinity that is the same as a column with a declared type of "type". 
     */
    public override DynamicType ExpressionType => _functionName switch
    {
        "typeof" => DynamicType.Text,
        _ => _args[0].ExpressionType
    };

    public override string ResultSetName =>
        $"{_functionName}({string.Join(", ", _args.Select(a => a.ResultSetName))})";
}