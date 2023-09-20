namespace FakeRdb;

public sealed record QueryResult(Field[] Schema, List<List<object?>> Data) : IResult
{
    public void Sort(OrderByClause orderBy)
    {
        Data.Sort(orderBy.GetComparer(Schema));
    }
}