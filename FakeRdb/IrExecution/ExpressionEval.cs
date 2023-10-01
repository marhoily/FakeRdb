using static FakeRdb.IR;

namespace FakeRdb;

public static class ExpressionEval
{
    public static T Eval<T>(this IExpression exp, Table table, int rowIndex)
    {
        var eval = exp.Eval(table, rowIndex)!;
        if (typeof(T) == typeof(bool))
        {
            return (T)((object?)eval.ToNullableBool() ?? false);
        }
        return (T)eval;
    }

    public static object? Eval(this IExpression arg, TypeAffinity targetAffinity)
    {
        return arg switch
        {
            BinaryExp binaryExp => binaryExp.Eval(targetAffinity),
            BindExp bindExp => bindExp.Value.CoerceToStoredType(),
            LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(targetAffinity),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }

    public static object? Eval(this IExpression arg, Table table, int rowIndex)
    {
        return arg switch
        {
            BindExp bind => bind.Value,
            BinaryExp binaryExp => binaryExp.Eval(table, rowIndex),
            ColumnExp columnExp => table.Get(columnExp.FullColumnName).Rows[rowIndex],
            InExp inExp => inExp.Eval(table, rowIndex),
            LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(TypeAffinity.NotSet),
            ScalarExp scalarExp => scalarExp.Function(table, rowIndex, scalarExp.Args),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }

    private static object? Eval(this BinaryExp arg, TypeAffinity targetAffinity)
    {
        var l = arg.Left.Eval(targetAffinity);
        var r = arg.Right.Eval(targetAffinity);
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

        var result = Calc(arg.Operand, l.Coerce(coerceTo), r.Coerce(coerceTo));

        /*
         * Any operators applied to column names, including the no-op unary "+" operator,
         * convert the column name into an expression which always has no affinity.
         * Hence even if X and Y.Z are column names, the expressions +X
         * and +Y.Z are not column names and have no affinity.
         */
        result.GetTypeAffinity();
        return result;

        // BUG: GetPriority is probably a wrong approach
        static int GetPriority(IExpression exp) =>
            exp switch
            {
                BinaryExp => 0,
                ColumnExp => 1,
                LiteralExp => 0,
                BindExp => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(exp))
            };

        static object? Calc(BinaryOperator op, object? x, object? y)
        {
            if (x == null || y == null)
                return null;
            if ((op & (BinaryOperator.IsComparison | BinaryOperator.IsLogical)) != 0)
                return CalcComparison(op, x, y) ? 1L : 0L;
            return (object?)(op switch
            {
                BinaryOperator.Multiplication => (dynamic)x * (dynamic)y,
                BinaryOperator.Addition => (dynamic)x + (dynamic)y,
                BinaryOperator.Concatenation => string.Concat(x, y),
                _ => throw new ArgumentOutOfRangeException(op.ToString())
            });
        }

        static bool CalcComparison(BinaryOperator op, object x, object y)
        {
            return op switch
            {
                BinaryOperator.Equal => Equals(x, y),
                BinaryOperator.Less => CustomFieldComparer.Compare(x, y) < 0,
                BinaryOperator.And => x.ToBool() && y.ToBool(),
                BinaryOperator.Or => x.ToBool() || y.ToBool(),
                _ => throw new ArgumentOutOfRangeException(op.ToString())
            };
        }
    }

    private static object Eval(this InExp arg, Table table, int rowIndex)
    {
        var n = arg.Needle.Eval(table, rowIndex);
        var nn = n.Coerce(arg.Haystack.Header.ColumnType);
        return arg.Haystack.Rows.Any(f => Equals(nn, f));
    }
}
