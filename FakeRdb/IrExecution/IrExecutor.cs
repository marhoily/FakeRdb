using static FakeRdb.IR;

namespace FakeRdb;

public static class IrExecutor
{
    public static Table Execute(this Database db, SelectStmt stmt)
    {
        // If it's just a core query, we must directly
        // execute it with the ordering terms
        if (stmt.Query is SelectCore core)
            return core.Execute(stmt.OrderingTerms);

        // If there are multiple cores connected by "UNION" or "EXCEPT"
        // execute all of them without ordering terms first...
        return db.ExecuteCompound(stmt.Query)
            // ...and then do the ordering.
            .OrderBy(stmt.OrderingTerms);

    }

    private static Table Execute(this SelectCore query, params OrderingTerm[] orderingTerms)
    {
        return query.Columns.Any(c => c.Exp is AggregateExp)
            ? AggregateSelectExecutor.SelectAggregate(query.From.Single(), query.Columns, query.GroupBy)
            : SelectExecutor.Select(query.From, query.Columns, query.Where, orderingTerms);
    }
    private static Table ExecuteCompound(this Database db, ICompoundSelect query)
    {
        if (query is SelectCore core)
            return core.Execute();
        if (query is CompoundSelect compound)
        {
            var left = db.ExecuteCompound(compound.Left);
            var right = db.ExecuteCompound(compound.Right);
            return compound.Operator switch
            {
                CompoundOperator.Union => Table.Union(left, right),
                CompoundOperator.UnionAll => Table.UnionAll(left, right),
                CompoundOperator.Intersect => Table.Intersect(left, right),
                CompoundOperator.Except => Table.Except(left, right),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        throw new ArgumentOutOfRangeException();
    }


    public static IResult PostProcess(this IResult result)
    {
        if (result is not QueryResult q) return result;

        var columns = q.Table.Columns.Select(col =>
        {
            if (col.Header.ColumnType != TypeAffinity.NotSet) return col;
            var affinity = col.Rows.FirstOrDefault().GetTypeAffinity();
            var newHeader = col.Header with { ColumnType = affinity };
            return col with { Header = newHeader };
        });
        return q with { Table = new Table(columns.ToArray())};

    }
}