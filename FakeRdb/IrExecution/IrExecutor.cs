using static FakeRdb.CompoundOperator;
using static FakeRdb.IR;

namespace FakeRdb;

public static class IrExecutor
{
    public static Table ExecuteStmt(this Database db, SelectStmt stmt, bool explain)
    {
        // If it's just a core query, we must directly
        // execute it with the ordering terms
        if (stmt.Query is SelectCore core)
            return core.ExecuteCore(explain, stmt.OrderingTerms);

        // If there are multiple cores connected by "UNION" or "EXCEPT"
        // execute all of them without ordering terms first...
        return db.ExecuteCompound(stmt.Query, explain)
            // ...and then do the ordering.
            .OrderBy(stmt.OrderingTerms);
    }

    private static Table ExecuteCompound(this Database db, ICompoundSelect query, bool explain)
    {
        if (query is SelectCore core)
            return core.ExecuteCore(explain);
        if (query is CompoundSelect compound)
        {
            var left = db.ExecuteCompound(compound.Left, explain);
            var right = db.ExecuteCompound(compound.Right, explain);
            return compound.Operator switch
            {
                Union => Table.Union(left, right),
                UnionAll => Table.UnionAll(left, right),
                Intersect => Table.Intersect(left, right),
                Except => Table.Except(left, right),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        throw new ArgumentOutOfRangeException();
    }

    private static Table ExecuteCore(this SelectCore query,
        bool explain,
        params OrderingTerm[] orderingTerms)
    {
        var singleSource = query.AlternativeSources.Single();
        var tables = singleSource
            .SingleTableConditions
            .Select(c => c.Table.Filter(c.Filter)).ToArray();
        if (tables.Length == 0)
            return ExecuteNoFrom(query.Columns, explain);
        var product = JoinTables(tables,
            singleSource.EquiJoinConditions, explain);

        if (singleSource.GeneralCondition != null)
            product.ApplyFilter(singleSource.GeneralCondition);

        var grouped = product.GroupBy(query.GroupBy
            .Select(product.Get).ToArray(), query.Columns);

        // We cannot take sorting out of SelectCore to the later stages
        // because the projection can throw the key columns away
        var ordered = grouped.OrderBy(orderingTerms);
        if (explain) return ordered;
        return ordered.Project(query.Columns);
    }

    private static Table ExecuteNoFrom(ResultColumn[] queryColumns, bool explain)
    {
        if (explain)
            return new ExplainTable()
                .With("SCAN CONSTANT ROW")
                .Build();
        return new Table("Result", queryColumns.Select((col, n) =>
        {
            var eval = col.Exp.Eval(TypeAffinity.NotSet);
            var name = col.Alias ?? col.Original;
            var affinity = eval.GetTypeAffinity();
            return new Column(
                new ColumnHeader(n, name, "Result." + name, affinity),
                new List<object?> { eval });
        }).ToArray());
    }

    /// <summary>
    /// Joins multiple tables using specified equi-join conditions.
    /// An equi-join matches rows from two tables based on the equality between specified columns from each table.
    /// Using equi-joins improves performance, as it allows targeted row scans or even direct jumps to specific rows using an index.
    /// If no equi-join condition is provided for a pair of tables, the method defaults to performing a Cartesian product.
    /// A Cartesian product pairs each row from the first table with every row from the second, potentially resulting in a large number of rows.
    /// </summary>
    /// <remarks>
    /// This implementation uses nested loop joins for combining rows. Nested loop joins are simple and work well when one of the tables is small or when the joined columns are indexed. 
    /// However, they can be inefficient for large data-sets or complex join conditions.
    /// Other join algorithms like "hash joins" or "sort-merge joins" could offer better performance for certain scenarios but are not implemented here.
    /// </remarks>
    private static Table JoinTables(Table[] tables, EquiJoinCondition[] equiJoins, bool explain)
    {
        switch (tables)
        {
            case []:
                return Table.Empty;
            case [var t]:
                if (explain)
                    return new ExplainTable()
                        .With($"SCAN {t.Name}")
                        .Build();
                return t.Clone();
            case [var head, .. var tail] _:
                return JoinRemainingTablesRecursively(head, tail);
            default:
                throw new ArgumentOutOfRangeException(nameof(tables));
        }

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