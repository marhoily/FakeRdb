using FsCheck.Xunit;
using static FakeRdb.IR;

namespace FakeRdb.Fuzzing;

public sealed class ConditionAnalyzerFuzzing
{
    [Property(
        Arbitrary = new[] { typeof(ExpressionGenerators) },
        EndSize = 100)]
    public void DiscriminateCondition(IExpression expr)
    {
        ConditionAnalyzer
            .DiscriminateCondition(expr)
            .Should().NotBeNull();
    }
}