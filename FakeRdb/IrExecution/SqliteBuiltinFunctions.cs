using System.Diagnostics.CodeAnalysis;
using static FakeRdb.IR;

namespace FakeRdb;

public static class SqliteBuiltinFunctions
{
    public static readonly IComparer<object?> Comparer = new ObjectComparer();

    public static object? Max(Table table, IEnumerable<int> rowSet, IExpression[] args)
    {
        var expression = args.Single();
        return rowSet
            .Select(rowIndex => expression.Eval(table, rowIndex))
            .Max(Comparer);
    }

    public static object? Min(Table table, IEnumerable<int> rowSet, IExpression[] args)
    {
        var expression = args.Single();
        return rowSet
            .Select(rowIndex => expression.Eval(table, rowIndex))
            .Min(Comparer);
    }
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static object? Sum(Table table, IEnumerable<int> rowSet, IExpression[] args)
    {
        if (!rowSet.Any()) return null;
        var expression = args.Single();
        return rowSet
            .Select(rowIndex => expression.Eval(table, rowIndex))
            .Select(x => x.CoerceToRealOrZero())
            .Sum();
    }
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static object? Avg(Table table, IEnumerable<int> rowSet, IExpression[] args)
    {
        if (!rowSet.Any()) return null;
        var expression = args.Single();
        return rowSet
            .Select(rowIndex => expression.Eval(table, rowIndex))
            .Select(x => x.CoerceToRealOrZero())
            .Average();
    }
    
    public static string TypeOf(Table table, int rowIndex, IExpression[] args)
    {
        var exp = (ColumnExp)args.Single();
        var resolve = exp.Eval(table, rowIndex);
        var affinity = table.Get(exp.FullColumnName).Header.ColumnType;
        var result = resolve.GetStorageType(affinity);
        return result.ToString().ToLowerInvariant();
    }

}