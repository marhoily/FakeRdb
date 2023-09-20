namespace FakeRdb;

public sealed class BinaryExpression : IExpression
{
    private readonly IExpression _left;
    private readonly Operator _op;
    private readonly IExpression _right;
    private SqliteTypeAffinity? _expressionType;

    public BinaryExpression(Operator op,
        IExpression left, IExpression right,
        string exp)
    {
        _left = left;
        _op = op;
        _right = right;
        ResultSetName = exp;
    }

    public SqliteTypeAffinity ExpressionType =>
        /*
         * Any operators applied to column names,
         * including the no-op unary "+" operator,
         * convert the column name into an expression
         * which always has no affinity.
         * 
         * Hence even if X and Y.Z are column names, the expressions +X and +Y.Z are not column names and have no affinity. 
         */
        _expressionType ??
        throw new InvalidOperationException(
            "Cannot determine ExpressionType of a binary operation before it was resolved");

    public string ResultSetName { get; }

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
            ? _right.ExpressionType
            : _left.ExpressionType;

        var result = Calc(_op, l.Coerce(coerceTo), r.Coerce(coerceTo));
        _expressionType = result.GetTypeAffinity();
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
                _ => throw new ArgumentOutOfRangeException(op.ToString())
            });
        }
    }
}