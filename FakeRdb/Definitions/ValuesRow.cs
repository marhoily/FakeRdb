namespace FakeRdb;

public sealed record ValuesRow(Expression[] Cells) : IResult;