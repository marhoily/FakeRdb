using static FakeRdb.IR;

namespace FakeRdb;

public static class Expr
{
    public static IExpression? And(IExpression? x, IExpression? y)
    {
        return (x, y) switch
        {
            (null, null) => null,
            (null, {}) => y,
            ({}, null) => x,
            _ => new BinaryExp(BinaryOperator.And, x, y)
        };
    }
}