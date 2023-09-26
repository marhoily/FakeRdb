namespace FakeRdb;

public interface IResult { }

public sealed record Affected(int RecordsCount) : IResult;

public sealed record QueryResult(
    ResultSchema Schema, 
    List<List<object?>> Data, 
    int RecordsCount = -1) : IResult;

public sealed record ResultSchema(ColumnDefinition[] Columns);
public sealed record ColumnDefinition(string Name, TypeAffinity ColumnType);