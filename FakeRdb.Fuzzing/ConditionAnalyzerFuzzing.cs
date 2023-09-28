using FsCheck.Xunit;
using static FakeRdb.IR;

namespace FakeRdb.Fuzzing;

public class ConditionAnalyzerFuzzing
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ConditionAnalyzerFuzzing(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Property(Arbitrary = new [] { typeof(ExpressionGenerators) })]
    public void DiscriminateCondition(IExpression expr)
    {
        try
        {
            ConditionAnalyzer.DiscriminateCondition(expr)
             .Should().NotBeNull();
        }
        catch (Exception)
        {
            _testOutputHelper.WriteLine(expr.Print());
            throw;
        }
    }
}