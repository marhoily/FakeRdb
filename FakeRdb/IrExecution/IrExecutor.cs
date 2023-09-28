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
        var singleSource = query.AlternativeSources.Single();
        var product = JoinTables(
            singleSource
                .SingleTableConditions
                .Select(c => c.Table.Filter(c.Filter)).ToArray(),
            singleSource.EquiJoinConditions);

        if (singleSource.GeneralCondition != null)
            product.ApplyFilter(singleSource.GeneralCondition);

        var grouped = product.GroupBy(query.GroupBy
            .Select(product.Get).ToArray(), query.Columns);

        // We cannot take sorting out of SelectCore to the later stages
        // because the projection can throw the key columns away
        return grouped.OrderBy(orderingTerms).Project(query.Columns);
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

    /// <summary>
    /// Joins multiple tables using specified equi-join conditions.
    /// An equi-join is a type of join where we combine rows from two tables based on the equality between two columns from each table.
    /// This is useful for associating rows that contain related data, improving query performance, and simplifying data retrieval.
    /// If no equi-join condition is specified for a pair of tables, the method falls back to performing a Cartesian product.
    /// A Cartesian product combines each row from the first table with each row from the second, which can result in a large number of rows.
    /// </summary>
    private static Table JoinTables(Table[] tables, EquiJoinCondition[] equiJoins)
    {
        return tables switch
        {
            [] => Table.Empty,
            [var t] => t.Clone(),
            [var head, .. var tail] _ => JoinRemainingTablesRecursively(head, tail)
        };

        Table JoinRemainingTablesRecursively(Table head, Table[] tail)
        {
            if (tail.Length == 0) return head;

            var tableToJoinNext = tail[0];
            var remainingTables = tail.Skip(1).ToArray();
            var condition = equiJoins.FindFor(head, tableToJoinNext);
            var result = head.ConcatHeaders(tableToJoinNext);

            if (condition == null) // fallback
                return CartesianProduct(result, head, tableToJoinNext, remainingTables);

            var leftColumnIndex = head.IndexOf(condition.LeftColumn);
            return result.WithRows(
                from headRow in head.GetRows()
                let right = JoinRemainingTablesRecursively(tableToJoinNext, remainingTables)
                let rightColumnIndex = right.IndexOf(condition.RightColumn)
                from tailRow in right.GetRows()
                where CustomFieldComparer.Equals(
                    headRow.Data[leftColumnIndex],
                    tailRow.Data[rightColumnIndex])
                select headRow.Concat(tailRow));
        }

        Table CartesianProduct(Table result, Table head, Table tableToJoinNext, Table[] remainingTables)
        {
            return result.WithRows(
                from headRow in head.GetRows()
                from tailRow in JoinRemainingTablesRecursively(tableToJoinNext, remainingTables).GetRows()
                select headRow.Concat(tailRow));
        }
    }

    private static EquiJoinCondition? FindFor(this EquiJoinCondition[] equiJoins, Table a, Table b)
    {
        return equiJoins.FirstOrDefault(
            e => (e.LeftTable == a && e.RightTable == b) ||
                 (e.LeftTable == b && e.RightTable == a));
    }
}