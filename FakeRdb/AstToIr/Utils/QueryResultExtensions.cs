namespace FakeRdb;

public static class QueryResultExtensions
{
    public static QueryResult Merge(this QueryResult q, Affected a)
    {
        var prev = q.RecordsCount == -1 ? 0 : q.RecordsCount;
        return q with { RecordsCount = prev + a.RecordsCount };
    }
}