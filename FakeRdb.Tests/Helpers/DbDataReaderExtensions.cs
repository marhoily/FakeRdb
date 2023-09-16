namespace FakeRdb.Tests;

//Assuming I want to compare the readers as well as possible, am I missing anything?
// #nullable enable
public static class DbDataReaderExtensions
{
    public static void ShouldEqual(this DbDataReader actual, DbDataReader expected)
    {
        actual.IsClosed.Should().BeFalse();
        expected.IsClosed.Should().BeFalse();
        expected.RecordsAffected.Should().Be(actual.RecordsAffected);
        expected.GetSchema().Should().BeEquivalentTo(
            actual.GetSchema(), opt => opt.WithStrictOrdering());
        expected.ReadData().Should().BeEquivalentTo(actual.ReadData(),
            opt => opt
                .WithStrictOrdering()
                .Using<double>(ctx => ctx.Subject.Should()
                    .BeApproximately(ctx.Expectation, 1e-4))
                .WhenTypeIs<double>()
                .Using<float>(ctx => ctx.Subject.Should()
                    .BeApproximately(ctx.Expectation, 1e-4f))
                .WhenTypeIs<float>());
    }

    private static IEnumerable<(Type, string)> GetSchema(this DbDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            yield return (reader.GetFieldType(i), reader.GetName(i));
        }
    }

    private static List<List<object?>> ReadData(this DbDataReader reader)
    {
        var rows = new List<List<object?>>();
        while (reader.Read())
        {
            var row = new List<object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row.Add(value == DBNull.Value ? null : value);
            }
            rows.Add(row);
        }
        return rows;
    }

}