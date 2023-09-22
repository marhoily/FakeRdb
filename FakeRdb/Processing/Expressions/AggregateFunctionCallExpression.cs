namespace FakeRdb;

public sealed class AggregateFunctionCallExpression : IExpression
{
    private readonly AggregateFunction _function;
    private readonly IExpression[] _args;

    public AggregateFunctionCallExpression(AggregateFunction function, IExpression[] args)
    {
        _function = function;
        _args = args;
    }

    public object Eval() => throw new NotSupportedException();
    public object Eval(Row dataSet)  => throw new NotSupportedException();
    public object Eval(Row[] dataSet) => _function(dataSet, _args);

    /*
     * An expression of the form "CAST(expr AS type)" has an affinity that is the same as a column with a declared type of "type".
     */
    //"typeof" => TypeAffinity.Text,

}