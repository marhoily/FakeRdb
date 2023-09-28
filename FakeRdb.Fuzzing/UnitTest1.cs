using FsCheck;
using FsCheck.Xunit;
using static FakeRdb.BinaryOperator;
using static FakeRdb.IR;

namespace FakeRdb.Fuzzing;
public class UnitTest1
{ 
    [Property(Arbitrary = new [] { typeof(ExpressionGenerators) })]
    public void YourTestMethod(IExpression expr)
    {
        ConditionAnalyzer.DiscriminateCondition(expr)
            .Should().NotBeNull();
    }
}


public static class ExpressionGenerators
{
    public static Arbitrary<IExpression> Expressions() => 
        AnyExpression().ToArbitrary();

    public static Gen<IExpression> AnyExpression() =>
        Gen.Sized(size => Gen.OneOf(
            Gen.Resize(size / 2, LiteralExpGen()),
            Gen.Resize(size / 2, BinaryExpGen())));

    private static Gen<IExpression> LiteralExpGen() =>
    from value in Gen.Elements("1", "2", "3", "'string'", "5.2")
    select (IExpression) new LiteralExp(value);

    private static Gen<BinaryOperator> AnyBinaryOperator() =>
        Gen.Elements(And, Or, Equal, Less);

    private static Gen<IExpression> BinaryExpGen() =>
        from op in AnyBinaryOperator()
        from left in AnyExpression()
        from right in AnyExpression()
        select (IExpression) new BinaryExp(op, left, right);
}
