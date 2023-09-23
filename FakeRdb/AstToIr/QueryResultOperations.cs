namespace FakeRdb;

public static class QueryResultOperations
{
    public static QueryResult Merge(this QueryResult q, Affected a)
    {
        var prev = q.RecordsCount == -1 ? 0 : q.RecordsCount;
        return q with { RecordsCount = prev + a.RecordsCount };
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