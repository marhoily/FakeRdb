namespace FakeRdb;

public static class X
{
    public static IExpression Convert(this IR.IExpression arg)
    {
        return arg switch
        {
            IR.BinaryExp binaryExp => new BinaryExpression(binaryExp.Op, Convert(binaryExp.Left), Convert(binaryExp.Right)),
            IR.BindExp bindExp => new ValueExpression(bindExp.Value, bindExp.Value.GetTypeAffinity()),
            IR.AggregateExp callExp => new AggregateFunctionCallExpression(callExp.Function, callExp.Args.Select(Convert).ToArray()),
            IR.ScalarExp callExp => new ScalarFunctionCallExpression(callExp.Function, callExp.Args.Select(Convert).ToArray()),
            IR.ColumnExp columnExp => new ProjectionExpression(columnExp.Value),
            IR.InExp inExp => new InExpression(inExp.Needle.Convert(), inExp.Haystack),
            IR.LiteralExp literalExp => new ValueExpression(literalExp.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }
    public static object? Eval(this IR.IExpression arg)
    {
        return arg switch {
            IR.AggregateExp aggregateExp => throw new NotImplementedException(),
            IR.BinaryExp binaryExp => binaryExp.Eval(),
            IR.BindExp bindExp => bindExp.Value,
            IR.ColumnExp columnExp => throw new NotImplementedException(),
            IR.InExp inExp => throw new NotImplementedException(),
            IR.LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(),
            IR.ScalarExp scalarExp => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }
    
    public static object? Eval(this IR.IExpression arg, Row row)
    {
        return arg switch {
            IR.AggregateExp aggregateExp => throw new NotImplementedException(),
            IR.BinaryExp binaryExp => binaryExp.Eval(row),
            IR.BindExp bindExp => bindExp.Value,
            IR.ColumnExp columnExp => row[columnExp.Value],
            IR.InExp inExp => inExp.Eval(row),
            IR.LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(),
            IR.ScalarExp scalarExp => scalarExp.Function(row, scalarExp.Args),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }
    
    public static object? Eval(this IR.IExpression arg, Row[] dataSet)
    {
        return arg switch {
            IR.AggregateExp aggregateExp => aggregateExp.Function(dataSet, aggregateExp.Args),
            IR.BindExp bindExp => bindExp.Value,
            IR.LiteralExp literalExp => literalExp.Value.CoerceToLexicalAffinity(),
            _ => throw new ArgumentOutOfRangeException(nameof(arg))
        };
    }
    
    public static object? Eval(this IR.BinaryExp arg)
    {
        var l = arg.Left.Eval();
        var r = arg.Right.Eval();
        return arg.Eval(l, r);
    }
    
    public static object? Eval(this IR.BinaryExp arg, Row row)
    {
        var l = arg.Left.Eval(row);
        var r = arg.Right.Eval(row);
        return arg.Eval(l, r);
    }

    private static object? Eval(this IR.BinaryExp arg, object? l, object? r)
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

        static int GetPriority(IR.IExpression exp) =>
            exp switch
            {
                IR.BinaryExp => 0,
                IR.ColumnExp => 1,
                IR.LiteralExp => 0,
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

    public static object Eval(this IR.InExp arg, Row dataSet)
    {
        var n = arg.Needle.Eval(dataSet);
        var nn = n.Coerce(arg.Haystack.Schema.Columns.Single().FieldType);
        return arg.Haystack.Data.Any(r => Equals(nn, r[0]));
    }
}