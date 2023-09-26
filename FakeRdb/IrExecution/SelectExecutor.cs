using static FakeRdb.IR;

namespace FakeRdb;

public static class SelectExecutor
{
    public static QueryResult Select(Table[] tables, ResultColumn[] columns, IExpression? where,
        OrderingTerm[] ordering)
    {
        var product = CartesianProduct(tables);
        if (where != null)
            product.ApplyFilter(where);

        // We cannot take sorting out of here to the later stages
        // because the projection can throw the key columns away
        var data = product.OrderBy(ordering).Project(columns);
        return new QueryResult(new ResultSchema(
            data.Schema.Select(c => c.ToDefinition()).ToArray()), data.ToList());
    }

    private static Table CartesianProduct(Table[] tables)
    {
        return tables.Length switch
        {
            0 => Table.Empty,
            1 => tables[0].Clone(),
            _ => Recurse(tables[0], tables.Skip(1).ToArray())
        };

        static Table Recurse(Table head, Table[] tail)
        {
            if (tail.Length == 0) return head;
            var result = head.ConcatColumns(tail[0]);
            result.AddRows(from headRow in head.GetRows()
                           from tailRow in Recurse(tail[0], tail.Skip(1).ToArray()).GetRows()
                           select headRow.Concat(tailRow));
            return result;
        }
    }
}