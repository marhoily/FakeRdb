using static FakeRdb.IR;

namespace FakeRdb;

public static class SelectExecutor
{
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