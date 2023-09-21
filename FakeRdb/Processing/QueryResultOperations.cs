namespace FakeRdb;

public static class QueryResultOperations
{
    public static IResult? PostProcess(this IResult? result)
    {
        if (result is not QueryResult q) return result;
        
        var columns = q.Schema.Columns;
        var firstRow = q.Data.FirstOrDefault();
        for (var i = 0; i < columns.Length; i++)
        {
            if (columns[i].FieldType != TypeAffinity.NotSet) continue;
            columns[i] = columns[i] with
            {
                FieldType = firstRow != null
                    ? firstRow[i].GetTypeAffinity()
                    : TypeAffinity.Blob
            };
        }
        return result;
    }
    public static void Sort(this QueryResult result, OrderByClause orderBy)
    {
        result.Data.Sort(orderBy.GetComparer(result.Schema));
    }
}