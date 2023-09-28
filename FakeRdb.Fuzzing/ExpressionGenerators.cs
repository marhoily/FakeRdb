using FsCheck;
using static FakeRdb.IR;

namespace FakeRdb.Fuzzing;

public static class ExpressionGenerators
{
    public static Arbitrary<IExpression> Expressions() => 
        AnyExpression().ToArbitrary();

    private static Gen<IExpression> AnyExpression() =>
        Gen.Sized(size => size > 5
            ? Gen.OneOf(LiteralExpGen(), Gen.Resize(size / 2, BinaryExpGen()))
            : LiteralExpGen());

    private static Gen<IExpression> LiteralExpGen() =>
        from value in Gen.Elements(
            "1", "'string'", "5.2", "true", "false", "NULL")
        select (IExpression) new LiteralExp(value);

    private static Gen<BinaryOperator> AnyBinaryOperator() =>
        Gen.Elements(BinaryOperator.And, BinaryOperator.Or, BinaryOperator.Equal, BinaryOperator.Less);

    private static Gen<IExpression> BinaryExpGen() =>
        from op in AnyBinaryOperator()
        from left in AnyExpression()
        from right in AnyExpression()
        select (IExpression) new BinaryExp(op, left, right);
}