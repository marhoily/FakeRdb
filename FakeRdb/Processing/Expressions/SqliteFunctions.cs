namespace FakeRdb;

public static class SqliteFunctions
{
    public static AggregateResult Max(Row[] dataSet, IExpression[] args)
    {
        var expression = args.Single();
        var row = dataSet.MaxBy(r => expression.Eval(r), TypeExt.Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row.Data, expression.Eval(row));
    }

    public static AggregateResult Min(Row[] dataSet, IExpression[] args)
    {
        var expression = args.Single();
        var row = dataSet.MinBy(r => expression.Eval(r), TypeExt.Comparer) ??
                  throw new NotImplementedException();
        return new AggregateResult(row.Data, expression.Eval(row));
    }

    public static string TypeOf(Row dataSet, IExpression[] args)
    {
        var exp = (ProjectionExpression)args.Single();
        var resolve = exp.Eval(dataSet);
        var affinity = exp.SelectColumn.FieldType;
        var result = resolve.GetStorageType(affinity);
        return result.ToString().ToLowerInvariant();
    }

}