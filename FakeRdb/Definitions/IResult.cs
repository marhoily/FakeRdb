namespace FakeRdb;

public interface IResult { }

public sealed record Affected(int RecordsCount) : IResult;

public sealed record AggregateResult(Row Row, object? Value);

public sealed record QueryResult(
    Field[] Schema, 
    List<List<object?>> Data) : IResult;

public sealed record ValuesTable(ValuesRow[] Rows) : IResult;

public sealed record ValuesRow(IExpression[] Cells);