namespace FakeRdb.Tests;

public record QueryPlan(string[] Items);

public static class ReaderResultExtensions
{
    public static (ReaderResult?, Exception?) SafeExecuteReader(this DbCommand cmd)
    {
        try
        {
            return (cmd.ExecuteReader().ToResult(), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    public static void ShouldEqual(this ReaderResult actual, ReaderResult expected)
    {
        actual.RecordsAffected.Should().Be(expected.RecordsAffected);

        actual.Schema.Should().BeEquivalentTo(
            expected.Schema, opt => opt.WithStrictOrdering());

        actual.Data.Should().BeEquivalentTo(expected.Data,
            opt => opt
                .WithStrictOrdering()
                .Using<double>(ctx => ctx.Subject.Should()
                    .BeApproximately(ctx.Expectation, 1e-4))
                .WhenTypeIs<double>()
                .Using<float>(ctx => ctx.Subject.Should()
                    .BeApproximately(ctx.Expectation, 1e-4f))
                .WhenTypeIs<float>());
    }

    public static void PlanShouldEqual(this ReaderResult actual, ReaderResult expected)
    {
        actual.RecordsAffected.Should().Be(expected.RecordsAffected);

        actual.Schema.Should().BeEquivalentTo(
            expected.Schema, opt => opt.WithStrictOrdering());

        actual.Data.ToQueryPlan().Should()
            .BeEquivalentTo(expected.Data.ToQueryPlan(),
            opt => opt.WithStrictOrdering());
    }
    public static QueryPlan ToQueryPlan(this List<List<object?>> data)
    {
        return new QueryPlan(data.Select(row => (string)row[3]!).ToArray());
    }
}