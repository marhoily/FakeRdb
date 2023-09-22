namespace FakeRdb;

public sealed class InExpression : IExpression
{
    private readonly IExpression _needle;
    private readonly QueryResult _set;

    public InExpression(IExpression needle, QueryResult set)
    {
        _needle = needle;
        _set = set;
    }

    public object Eval() => throw new NotSupportedException();

    public object Eval(Row[] dataSet) => throw new NotSupportedException();

    public object Eval(Row dataSet)
    {
        var n = _needle.Eval(dataSet);
        var nn = n.Coerce(_set.Schema.Columns.Single().FieldType);
        return _set.Data.Any(r => Equals(nn, r[0]));
    }
}