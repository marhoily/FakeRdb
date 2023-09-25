﻿using static FakeRdb.IR;

namespace FakeRdb;

public static class SelectExecutor
{
    public static QueryResult Select(Table[] tables, ResultColumn[] columns, IExpression? where,
        OrderingTerm[] ordering)
    {
        var projection = columns.Select(c => c.Exp).ToArray();
        var data = BuildData(tables, projection, where, ordering);
        var schema = BuildSchema(columns, projection);
        return new QueryResult(schema, data);
    }

    private static List<List<object?>> BuildData(Table[] source,
            IExpression[] selectors, IExpression? filter,
            OrderingTerm[] orderingTerms)
    {
        var product = CartesianProduct(source);
        if(filter != null)
            product.ApplyFilter(filter);
        var temp = product.GetRows().ToList();
            //ApplyFilter(product, filter).ToList();

        // We cannot take sorting out of here to the later stages
        // because the projection can throw the sorted columns a away
        ApplyOrdering(temp, orderingTerms);

        return ApplyProjection(temp, selectors);
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

    private static ResultSchema BuildSchema(ResultColumn[] columns, IExpression[] projection)
    {
        return new ResultSchema(columns.Zip(projection)
            .Select(column => new ColumnDefinition(
                column.First.Alias ??
                AsColumn(column.First.Exp)?.Name ??
                column.First.Original,
                AsColumn(column.First.Exp)?.ColumnType ??
                TypeAffinity.NotSet))
            .ToArray());

        ColumnHeader? AsColumn(IExpression exp) =>
            exp is ColumnExp col ? col.Value : null;
    }

    private static List<List<object?>> ApplyProjection(IEnumerable<Row> rows, IExpression[] selectors)
    {
        return rows
            .Select(row => selectors
                .Select(selector => selector.Eval(row))
                .ToList())
            .ToList();
    }

    private static void ApplyOrdering(List<Row> temp, OrderingTerm[] orderingTerms)
    {
        foreach (var orderingTerm in orderingTerms)
            temp.Sort(Row.Comparer(
                orderingTerm.Column.ColumnIndex));
    }


}