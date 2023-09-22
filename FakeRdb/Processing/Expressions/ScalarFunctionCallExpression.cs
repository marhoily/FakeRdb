namespace FakeRdb;

public sealed class ScalarFunctionCallExpression : IExpression
{
    private readonly ScalarFunction _function;
    private readonly IExpression[] _args;

    public ScalarFunctionCallExpression(ScalarFunction function, IExpression[] args)
    {
        _function = function;
        _args = args;
    }

    public object Eval() => throw new NotSupportedException();
    public object Eval(Row[] dataSet)  => throw new NotSupportedException();
    public object Eval(Row row) => _function(row, _args);

    /*
     * An expression of the form "CAST(expr AS type)" has an affinity that is the same as a column with a declared type of "type".
     */
}