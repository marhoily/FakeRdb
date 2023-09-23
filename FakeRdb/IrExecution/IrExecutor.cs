namespace FakeRdb;

public static class IrExecutor
{
    public static QueryResult Execute(this IR.SelectStmt stmt)
    {
        return Recursive(stmt.Query, stmt.OrderingTerms);
        static QueryResult Union(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
    
            // Use Distinct to remove duplicates. This assumes that List<object?> implements appropriate equality semantics.
            var resultData = x.Data.Concat(y.Data)
                .Distinct(new RowEqualityComparer<object?>())
                .Order(new RowByColumnComparer(0))
                .ToList();
    
            return new QueryResult(x.Schema, resultData);
        }


        static QueryResult Intersect(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
            var customComparer = new RowEqualityComparer<object?>();
            var resultData = x.Data
                .Intersect(y.Data, customComparer)
                .Order(new RowByColumnComparer(0))
                .ToList();
            return new QueryResult(x.Schema, resultData);
        }

        static QueryResult Except(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
    
            var customComparer = new RowEqualityComparer<object?>();
            var resultData = x.Data
                .Except(y.Data, customComparer)
                .Order(new RowByColumnComparer(0))
                .ToList();
    
            return new QueryResult(x.Schema, resultData);
        }
        static QueryResult UnionAll(QueryResult x, QueryResult y)
        {
            ValidateSchema(x.Schema, y.Schema);
            var resultData = new List<List<object?>>(x.Data);
            resultData.AddRange(y.Data);
            return new QueryResult(x.Schema, resultData);
        }

        static void ValidateSchema(ResultSchema x, ResultSchema y)
        {
            if (x.Columns.Length != y.Columns.Length)
            {
                throw new ArgumentException("SELECTs to the left and right of UNION do not have the same number of result columns");
            }
        }

        static QueryResult Recursive(IR.ICompoundSelect query, params IR.OrderingTerm[] orderingTerms)
        {
            if (query is IR.SelectCore core)
                return Terminal(core, orderingTerms);
            if (query is IR.CompoundSelect compound)
            {
                var left = Recursive(compound.Left);
                var right = Recursive(compound.Right);
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
        static QueryResult Terminal(IR.SelectCore query, params IR.OrderingTerm[] stmtOrderingTerms)
        {
            var orderingTerm = stmtOrderingTerms.FirstOrDefault();
            var aggregate = query.Columns
                .Where(c => c.Exp is IR.AggregateExp)
                .ToList();
            if (aggregate.Count > 0)
            {
                return query.From.SelectAggregate(aggregate);
            }

            var result = query.From.Select(query.Columns, query.Where);
            if (orderingTerm != null)
            {
                result.Data.Sort(new RowByColumnComparer(
                    result.Schema.IndexOf(orderingTerm.Column)));
            }
            return result;

        }
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