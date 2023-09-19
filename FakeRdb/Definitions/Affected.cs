namespace FakeRdb;

public sealed record Affected(int RecordsCount) : IResult;