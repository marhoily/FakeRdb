namespace FakeRdb;

public interface IResult { }

public sealed record Affected(int RecordsCount) : IResult;

public sealed record QueryResult(Table Table, int RecordsCount = -1) : IResult;