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

    protected override void SetTarget(Field field)
    {
        throw new NotImplementedException();
    }

    protected override void SetValue(object value)
    {
        throw new NotImplementedException();
    }

    public override object Resolve(params Row[] dataSet)
    {
        var n = _needle.Resolve(dataSet);
        var nn = Convert.ChangeType(n, _set.Schema.Single().FieldType);
        return _set.Data.Any(r => Equals(nn, r[0]));
    }

    public override Type ExpressionType => typeof(bool);
}