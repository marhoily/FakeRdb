namespace FakeRdb;

public static class SqliteBuiltinFunctions
{
    public static readonly IComparer<object?> Comparer = new ObjectComparer();

    public static object? Max(Row[] dataSet, IR.IExpression[] args)
    {
        var expression = args.Single();
        return dataSet.Select(expression.Eval).Max(Comparer);
    }

    public static object? Min(Row[] dataSet, IR.IExpression[] args)
    {
        var expression = args.Single();
        return dataSet.Select(expression.Eval).Min(Comparer);

    }

    public static object? Sum(Row[] dataSet, IR.IExpression[] args)
    {
        var first = dataSet.FirstOrDefault();
        if (first == null) return null;
        var expression = args.Single();
        return dataSet.Select(expression.Eval)
            .Select(x => x.CoerceToRealOrZero())
            .Sum();
    }
    public static object? Avg(Row[] dataSet, IR.IExpression[] args)
    {
        var first = dataSet.FirstOrDefault();
        if (first == null) return null;
        var expression = args.Single();
        return dataSet.Select(expression.Eval)
            .Select(x => x.CoerceToRealOrZero())
            .Average();
    }

    public static string TypeOf(Row dataSet, IR.IExpression[] args)
    {
        var exp = (IR.ColumnExp)args.Single();
        var resolve = exp.Eval(dataSet);
        var affinity = exp.Value.Header.ColumnType;
        var result = resolve.GetStorageType(affinity);
        return result.ToString().ToLowerInvariant();
    }

}