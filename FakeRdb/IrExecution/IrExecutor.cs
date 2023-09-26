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
        return Select(query.From, query.Columns, query.Where, query.GroupBy,orderingTerms);
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

    public static Table Select(Table[] tables,
        ResultColumn[] columns,
        IExpression? where,
        ColumnHeader[] groupBy,
        OrderingTerm[] ordering)
    {
        var product = CartesianProduct(tables);
        product = product.GroupBy(
            groupBy.Select(c => product.Columns[c.ColumnIndex]).ToArray(),
            columns);

        if (where != null)
            product.ApplyFilter(where);

        // We cannot take sorting out of here to the later stages
        // because the projection can throw the key columns away
        return product.OrderBy(ordering).Project(columns);
    }

    private static Table CartesianProduct(Table[] tables)
    {
        return tables switch
        {
            [] => Table.Empty,
            [var t] => t.Clone(),
            [var head, .. var tail] _ => Recurse(head, tail)
        };

        static Table Recurse(Table head, Table[] tail)
        {
            if (tail.Length == 0) return head;
            return head
                .ConcatHeaders(tail[0])
                .WithRows(from headRow in head.GetRows()
                    from tailRow in Recurse(tail[0], tail.Skip(1).ToArray()).GetRows()
                    select headRow.Concat(tailRow));
        }
    }
}