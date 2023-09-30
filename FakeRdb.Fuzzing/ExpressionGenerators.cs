using FsCheck;
using static FakeRdb.IR;

namespace FakeRdb.Fuzzing;

public static class ExpressionGenerators
{
    public static Arbitrary<IExpression> Expressions() => 
        AnyExpression().ToArbitrary();

    private static Gen<IExpression> AnyExpression() =>
        Gen.Sized(size => size > 5
            ? Gen.OneOf(SimpleExpressionGen(),
                Gen.Resize(size / 2, BinaryExpGen()))
            : Gen.OneOf(SimpleExpressionGen()));

    private static Gen<IExpression> SimpleExpressionGen() => 
        Gen.OneOf(LiteralExpGen(), BindExpGen(), ColumnExpGen());

    private static Gen<IExpression> BindExpGen() =>
        from value in Gen.Elements<object?>(
            15L, "str", 5.2, 0L, null, true, false, -6m,(short) 9)
        select (IExpression) new BindExp(value);

    private static Gen<IExpression> LiteralExpGen() =>
        from value in Gen.Elements(
            "1", "42.0", "5e2", "-7", "'string'", "5.2", "true", "false", "NULL")
        select (IExpression) new LiteralExp(value);

    private static Gen<IExpression> ColumnExpGen() =>
        from table in Gen.Elements(new Table("X"), new Table("Y"))
        from column in Gen.Elements("X.A", "X.B", "Y.B", "Y.C", "Y.D")
        select (IExpression) new ColumnExp(table, column);

    private static Gen<BinaryOperator> AnyBinaryOperator() =>
        Gen.Elements(BinaryOperator.And, BinaryOperator.Or, BinaryOperator.Equal, BinaryOperator.Less);

    private static Gen<IExpression> BinaryExpGen() =>
        from op in AnyBinaryOperator()
        from left in AnyExpression()
        from right in AnyExpression()
        select (IExpression) new BinaryExp(op, left, right);
}