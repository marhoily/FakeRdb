namespace FakeRdb;

public sealed class BinaryExpression : IExpression
{
    private readonly IExpression _left;
    private readonly Operator _op;
    private readonly IExpression _right;

    public BinaryExpression(Operator op,
        IExpression left, IExpression right)
    {
        _left = left;
        _op = op;
        _right = right;
    }

    public object? Eval()
    {
        var l = _left.Eval();
        var r = _right.Eval();
        return Eval(l, r);
    }

    // Binary operations are not defined ot Tables
    public object Eval(Row[] dataSet) => throw new NotSupportedException();

    public object? Eval(Row dataSet)
    {
        var l = _left.Eval(dataSet);
        var r = _right.Eval(dataSet);
        return Eval(l, r);
    }

    private object? Eval(object? l, object? r)
    {
        var coerceTo = GetPriority(_left) < GetPriority(_right)
            ? _right.GetTypeAffinity()
            : _left.GetTypeAffinity();

        var result = Calc(_op, l.Coerce(coerceTo), r.Coerce(coerceTo));
        
        /*
         * Any operators applied to column names, including the no-op unary "+" operator,
         * convert the column name into an expression which always has no affinity.
         * Hence even if X and Y.Z are column names, the expressions +X
         * and +Y.Z are not column names and have no affinity.
         */
        result.GetTypeAffinity();
        return result;

        static int GetPriority(IExpression exp) =>
            exp switch
            {
                BinaryExpression => 0,
                ProjectionExpression => 1,
                ValueExpression => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(exp))
            };

        static object? Calc(Operator op, object? x, object? y)
        {
            if (x == null || y == null)
                return null;
            return (object?)(op switch
            {
                Operator.Multiplication => (dynamic)x * (dynamic)y,
                Operator.Equal => Equals(x, y),
                Operator.Less => x is IComparable c ? c.CompareTo(y) == -1 : throw new NotSupportedException(),
                Operator.Addition => (dynamic)x + (dynamic)y,
                Operator.Concatenation => string.Concat(x, y),
                _ => throw new ArgumentOutOfRangeException(op.ToString())
            });
        }
    }
}