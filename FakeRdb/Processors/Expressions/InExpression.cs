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

    public object Eval(params Row[] dataSet)
    {
        var n = _needle.Eval(dataSet);
        var nn = n.Coerce(_set.Schema.Single().FieldType);
        return _set.Data.Any(r => Equals(nn, r[0]));
    }

    public SqliteTypeAffinity ExpressionType => SqliteTypeAffinity.Integer;
    public string ResultSetName => "???";
}