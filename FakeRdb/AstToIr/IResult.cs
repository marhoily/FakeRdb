namespace FakeRdb;

public interface IResult { }

public sealed record Affected(int RecordsCount) : IResult;

public sealed record AggregateResult(object?[] Row, object? Value);

public sealed record QueryResult(
    ResultSchema Schema, 
    List<List<object?>> Data, 
    int RecordsCount = -1) : IResult;

public sealed record ResultSchema(ColumnDefinition[] Columns);
public sealed record ColumnDefinition(string Name, TypeAffinity FieldType);

public sealed class OrderByClause : IResult
{
    private readonly Field _field;

    public OrderByClause(Field field) => _field = field;

    public IComparer<List<object?>> GetComparer(ResultSchema schema) =>
        new SelectiveComparer(schema.IndexOf(_field));

}