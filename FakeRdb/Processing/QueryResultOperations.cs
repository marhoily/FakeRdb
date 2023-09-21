namespace FakeRdb;

public static class QueryResultOperations
{
    public static void Sort(this QueryResult result, OrderByClause orderBy)
    {
        result.Data.Sort(orderBy.GetComparer(result.Schema));
    }
}