namespace FakeRdb;

public sealed class InExpression : Expression
{
    private readonly Expression _needle;
    private readonly QueryResult _set;

    public InExpression(Expression needle, QueryResult set)
    {
        _needle = needle;
        _set = set;
    }

    public override object Resolve(params Row[] dataSet)
    {
        var n = _needle.Resolve(dataSet);
        var nn = n.Coerce(_set.Schema.Single().FieldType);
        return _set.Data.Any(r => Equals(nn, r[0]));
    }

    public override SqliteTypeAffinity ExpressionType => SqliteTypeAffinity.Integer;
    public override string ResultSetName => "???";
}