namespace FakeRdb;

public sealed record ValuesTable(ValuesRow[] Rows) : IResult;