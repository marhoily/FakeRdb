namespace FakeRdb;

public sealed class FunctionCallExpression : IExpression
{
    private static readonly string[] AggregateFunctions =
    {
        "MAX", "MIN", "AVG"
    };
    private readonly string _functionName;
    private readonly IExpression[] _args;
    private string? _alias;

    public bool IsAggregate => AggregateFunctions.Contains(_functionName.ToUpperInvariant());

    public FunctionCallExpression(string functionName, IExpression[] args)
    {
        _functionName = functionName;
        _args = args;
    }

    public object Eval() => throw new NotSupportedException();
    public object Eval(Row dataSet) =>
        _functionName.ToUpperInvariant() switch
        {
            "TYPEOF" => TypeOf(dataSet),
            _ => throw new ArgumentOutOfRangeException("Unknown:" + _functionName)
        };
    public object Eval(Row[] dataSet) =>
        _functionName.ToUpperInvariant() switch
        {
            "MAX" => Max(dataSet),
            "MIN" => Min(dataSet),
            _ => throw new ArgumentOutOfRangeException("Unknown:" + _functionName)
        };

    private object TypeOf(Row dataSet)
    {
        var exp = (ProjectionExpression)_args.Single();
        var resolve = exp.Eval(dataSet);
        var affinity = exp.SelectColumn.FieldType;
        var result = resolve.GetStorageType(affinity);
        return result.ToString().ToLowerInvariant();
    }

    private AggregateResult Max(Row[] dataSet)
    {
        var expression = _args.Single();
        var row = dataSet.MaxBy(r => expression.Eval(r), TypeExt.Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row.Data, expression.Eval(row));
    }
    private AggregateResult Min(Row[] dataSet)
    {
        var expression = _args.Single();
        var row = dataSet.MinBy(r => expression.Eval(r), TypeExt.Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row.Data, expression.Eval(row));
    }

    /*
     * An expression of the form "CAST(expr AS type)" has an affinity that is the same as a column with a declared type of "type". 
     */
    public TypeAffinity ExpressionType => _functionName switch
    {
        "typeof" => TypeAffinity.Text,
        _ => _args[0].ExpressionType
    };

    public void SetAlias(string value) => _alias = value;
    public string ResultName => _alias ??
        $"{_functionName}({string.Join(", ", _args.Select(a => a.ResultName))})";
}