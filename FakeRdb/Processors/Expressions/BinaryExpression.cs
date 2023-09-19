namespace FakeRdb;

public sealed class BinaryExpression : Expression
{
    private readonly Expression _left;
    private readonly Operator _op;
    private readonly Expression _right;

    public BinaryExpression(Expression left, Operator op, Expression right)
    {
        _left = left;
        _op = op;
        _right = right;
    }

    protected override void SetTarget(Field field)
    {
        _left.BindTarget(field);
        _right.BindTarget(field);
    }

    protected override void SetValue(object value)
    {
        _left.BindValue(value);
        _right.BindValue(value);
    }

    public override object? Resolve(Row row)
    {
        dynamic l = _left.Resolve(row) ?? throw new InvalidOperationException();
        dynamic r = _right.Resolve(row) ?? throw new InvalidOperationException();
        return _op switch
        {
            Operator.Mul => l * r,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}