using FsCheck.Xunit;
using static FakeRdb.IR;

namespace FakeRdb.Fuzzing;

public sealed class ConditionAnalyzerFuzzing
{
    [Property(
        Arbitrary = new[] { typeof(ExpressionGenerators) },
        EndSize = 100, Replay = "193158020,297238713")]
    public void BuildAlternativeSources(IExpression expr)
    {
        ConditionAnalyzer
            .BuildAlternativeSources(Enumerable.Empty<Table>(), expr)
            .Should().NotBeNull();
    }
}