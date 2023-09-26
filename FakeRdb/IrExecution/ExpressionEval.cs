using static FakeRdb.IR;

namespace FakeRdb;

public static class ExpressionEval
{
    public static T Eval<T>(this IExpression exp, Row[] dataSet) => (T)exp.Eval(dataSet)!;
    public static T Eval<T>(this IExpression exp, Row dataSet) => (T)exp.Eval(dataSet)!;

    public static object? Eval(this IExpression arg)
    {
        return arg switch
        {
            BinaryExp binaryExp => binaryExp.Eval(),
            BindExp bindExp => bindExp.Value,
            LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }

    public static object? Eval(this IExpression arg, Row row)
    {
        return arg switch
        {
            BinaryExp binaryExp => binaryExp.Eval(row),
            ColumnExp columnExp => row[columnExp.Value.Header],
            InExp inExp => inExp.Eval(row),
            LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(),
            ScalarExp scalarExp => scalarExp.Function(row, scalarExp.Args),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }
    public static object? Eval(this IExpression arg, Table table, int rowIndex)
    {
        return arg switch
        {
            BinaryExp binaryExp => binaryExp.Eval(table, rowIndex),
            ColumnExp columnExp => columnExp.Value.Rows[rowIndex],
            InExp inExp => inExp.Eval(table, rowIndex),
            LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(),
            ScalarExp scalarExp => scalarExp.Function(table.GetRow(rowIndex), scalarExp.Args),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }

    private static object? Eval(this IExpression arg, Row[] dataSet)
    {
        return arg switch
        {
            AggregateExp aggregateExp => aggregateExp.Function(dataSet, aggregateExp.Args),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }

    private static object? Eval(this BinaryExp arg)
    {
        var l = arg.Left.Eval();
        var r = arg.Right.Eval();
        return arg.Eval(l, r);
    }

    private static object? Eval(this BinaryExp arg, Row row)
    {
        var l = arg.Left.Eval(row);
        var r = arg.Right.Eval(row);
        return arg.Eval(l, r);
    }
    private static object? Eval(this BinaryExp arg, Table table, int rowIndex)
    {
        var l = arg.Left.Eval(table, rowIndex);
        var r = arg.Right.Eval(table, rowIndex);
        return arg.Eval(l, r);
    }

    private static object? Eval(this BinaryExp arg, object? l, object? r)
    {
        var coerceTo = GetPriority(arg.Left) < GetPriority(arg.Right)
            ? arg.Left.GetTypeAffinity()
            : arg.Right.GetTypeAffinity();

        var result = Calc(arg.Op, l.Coerce(coerceTo), r.Coerce(coerceTo));

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
                BinaryExp => 0,
                ColumnExp => 1,
                LiteralExp => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(exp))
            };

        static object? Calc(BinaryOperator op, object? x, object? y)
        {
            if (x == null || y == null)
                return null;
            return (object?)(op switch
            {
                BinaryOperator.Multiplication => (dynamic)x * (dynamic)y,
                BinaryOperator.Equal => Equals(x, y),
                BinaryOperator.Less => x is IComparable c ? c.CompareTo(y) == -1 : throw new NotSupportedException(),
                BinaryOperator.Addition => (dynamic)x + (dynamic)y,
                BinaryOperator.Concatenation => string.Concat(x, y),
                _ => throw new ArgumentOutOfRangeException(op.ToString())
            });
        }
    }

    private static object Eval(this InExp arg, Row dataSet)
    {
        var n = arg.Needle.Eval(dataSet);
        var nn = n.Coerce(arg.Haystack.Schema.Columns.Single().ColumnType);
        return arg.Haystack.Data.Any(r => Equals(nn, r[0]));
    }
    private static object Eval(this InExp arg, Table table, int rowIndex)
    {
        var n = arg.Needle.Eval(table, rowIndex);
        var nn = n.Coerce(arg.Haystack.Schema.Columns.Single().ColumnType);
        return arg.Haystack.Data.Any(r => Equals(nn, r[0]));
    }

    public static object? Invoke(this AggregateExp arg)
    {
        throw new NotImplementedException();
    }
}
