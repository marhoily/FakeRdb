using static FakeRdb.IR;

namespace FakeRdb;

public static class IrExecutor
{
    public static QueryResult Execute(this Database db, SelectStmt stmt)
    {
        // If it's just a core query, we must directly
        // execute it with the ordering terms
        if (stmt.Query is SelectCore core)
            return core.Execute(stmt.OrderingTerms);

        // If there are multiple cores connected by "UNION" or "EXCEPT"
        // execute all of them without ordering terms first...
        var result = db.ExecuteCompound(stmt.Query);
        // ...and then do the ordering.
        foreach (var orderingTerm in stmt.OrderingTerms)
            result.Data.Sort(Row.Comparer(
                result.Schema.IndexOf(orderingTerm.Column)));
        return result;

    }

    private static QueryResult Execute(this SelectCore query, params OrderingTerm[] orderingTerms)
    {
        return query.Columns.Any(c => c.Exp is AggregateExp)
            ? AggregateSelectExecutor.SelectAggregate2(query.From.Single(), query.Columns, query.GroupBy)
            : SelectExecutor.Select(query.From, query.Columns, query.Where, orderingTerms);
    }
    private static QueryResult ExecuteCompound(this Database db, ICompoundSelect query)
    {
        if (query is SelectCore core)
            return core.Execute();
        if (query is CompoundSelect compound)
        {
            var left = db.ExecuteCompound(compound.Left);
            var right = db.ExecuteCompound(compound.Right);
            return compound.Operator switch
            {
                CompoundOperator.Union => Union(left, right),
                CompoundOperator.UnionAll => UnionAll(left, right),
                CompoundOperator.Intersect => Intersect(left, right),
                CompoundOperator.Except => Except(left, right),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        throw new ArgumentOutOfRangeException();
    }

    private static void ValidateSchema(ResultSchema x, ResultSchema y)
    {
        if (x.Columns.Length != y.Columns.Length)
        {
            throw new ArgumentException("SELECTs to the left and right of UNION do not have the same number of result columns");
        }
    }
    private static QueryResult Union(QueryResult x, QueryResult y)
    {
        ValidateSchema(x.Schema, y.Schema);

        // Use Distinct to remove duplicates. This assumes that List<object?> implements appropriate equality semantics.
        var resultData = x.Data.Concat(y.Data)
            .Distinct(Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();

        return new QueryResult(x.Schema, resultData);
    }
    private static QueryResult Intersect(QueryResult x, QueryResult y)
    {
        ValidateSchema(x.Schema, y.Schema);
        var resultData = x.Data
            .Intersect(y.Data, Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();
        return new QueryResult(x.Schema, resultData);
    }
    private static QueryResult Except(QueryResult x, QueryResult y)
    {
        ValidateSchema(x.Schema, y.Schema);

        var resultData = x.Data
            .Except(y.Data, Row.EqualityComparer)
            .Order(Row.Comparer(0))
            .ToList();

        return new QueryResult(x.Schema, resultData);
    }
    private static QueryResult UnionAll(QueryResult x, QueryResult y)
    {
        ValidateSchema(x.Schema, y.Schema);
        var resultData = new List<List<object?>>(x.Data);
        resultData.AddRange(y.Data);
        return new QueryResult(x.Schema, resultData);
    }

    public static IResult PostProcess(this IResult result)
    {
        if (result is not QueryResult q) return result;

        var columns = q.Schema.Columns;
        var firstRow = q.Data.FirstOrDefault();
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].ColumnType != TypeAffinity.NotSet) continue;
            columns[i] = columns[i] with
            {
                ColumnType = firstRow != null
                    ? firstRow[i].GetTypeAffinity()
                    : TypeAffinity.Blob
            };
        }
        return result;
    }
}