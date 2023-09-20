namespace FakeRdb;

public sealed record ValuesRow(IExpression[] Cells) : IResult;