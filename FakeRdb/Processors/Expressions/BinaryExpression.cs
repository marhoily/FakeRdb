namespace FakeRdb;

public sealed class BinaryExpression : Expression
{
    private readonly Expression _left;
    private readonly Operator _op;
    private readonly Expression _right;
    private SqliteTypeAffinity? _expressionType;

    public BinaryExpression(Operator op, 
        Expression left, Expression right,
        string exp)
    {
        _left = left;
        _op = op;
        _right = right;
        ResultSetName = exp;
    }

    protected override void SetTarget(Field field)
    {
        _left.BindTarget(field);
        _right.BindTarget(field);
    }

    public override SqliteTypeAffinity ExpressionType =>
        /*
         *Any operators applied to column names, including the no-op unary "+" operator, convert the column name into an expression which always has no affinity. Hence even if X and Y.Z are column names, the expressions +X and +Y.Z are not column names and have no affinity. 
         */
        _expressionType ??
        throw new InvalidOperationException(
            "Cannot determine ExpressionType of a binary operation before it was resolved");

    public override string ResultSetName { get; }

    public override object? Resolve(params Row[] dataSet)
    {
        var l = _left.Resolve(dataSet);
        var r = _right.Resolve(dataSet);
        if (GetCoercionPriority(_left) < GetCoercionPriority(_right))
        {
            _expressionType = _right.ExpressionType;
            l = l.Coerce(_right.ExpressionType);
        }
        else
        {
            _expressionType = _left.ExpressionType;
            r = r.Coerce(_left.ExpressionType);
        }

        return _op switch
        {
            Operator.Multiplication => (l, r) switch
            {
                // TODO: move to type coercion code
                (null, _) or (_, null) => null,
                (double a, decimal b) => a * (double) b,
                (decimal a, double b) => (double) a * b,
                _ => (dynamic)l * (dynamic)r,
            },
            Operator.Equal => Equals(l, r),
            Operator.Less => l is IComparable c ? c.CompareTo(r) == -1 : throw new NotSupportedException(),
            Operator.Addition => l == null || r == null ? null : (dynamic)l + (dynamic)r,
            _ => throw new ArgumentOutOfRangeException(_op.ToString())
        };

        static int GetCoercionPriority(Expression exp) =>
            exp switch
            {
                BinaryExpression => 0,
                FieldAccessExpression => 1,
                ValueExpression => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(exp))
            };
    }

}