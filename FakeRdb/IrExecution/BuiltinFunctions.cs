namespace FakeRdb;

public static class BuiltinFunctions
{
    public static readonly IComparer<object?> Comparer = new ObjectComparer();

    public static AggregateResult Max(Row[] dataSet, IR.IExpression[] args)
    {
        var expression = args.Single();
        var row = dataSet.MaxBy(expression.Eval, Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row.Data, expression.Eval(row));
    }

    public static AggregateResult Min(Row[] dataSet, IR.IExpression[] args)
    {
        var expression = args.Single();
        var row = dataSet.MinBy(expression.Eval, Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row.Data, expression.Eval(row));
    }

    public static string TypeOf(Row dataSet, IR.IExpression[] args)
    {
        var exp = (IR.ColumnExp)args.Single();
        var resolve = exp.Eval(dataSet);
        var affinity = exp.Value.FieldType;
        var result = resolve.GetStorageType(affinity);
        return result.ToString().ToLowerInvariant();
    }

}