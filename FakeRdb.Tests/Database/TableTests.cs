using static FakeRdb.IR;

namespace FakeRdb.Tests;

public sealed class TableTests
{
    private readonly Table _table = new(new[]
    {
        new ColumnHeader(0, "X", TypeAffinity.NotSet),
        new ColumnHeader(1, "Y", TypeAffinity.NotSet)
    });

    [Fact]
    public void GroupBy()
    {
        _table.Add(7, "a");
        _table.Add(8, "b");
        _table.Add(7, "c");
        var result = _table.GroupBy(new[] { _table.Columns[1] },
            new[]
            {
                new ResultColumn(
                    new ColumnExp(_table.Columns[0]), "A")
            });
        result.GetRows().Should().BeEquivalentTo(
            new []{new Row(7), new Row(8), new Row(7)},
            cfg => cfg.WithStrictOrdering());
    }
    [Fact]
    public void GroupBy_WithAggregate()
    {
        _table.Add(7, "a");
        _table.Add(8, "b");
        _table.Add(7, "c");
        var result = _table.GroupBy(new[] { _table.Columns[1] },
            new[]
            {
                new ResultColumn(
                    new AggregateExp(SqliteBuiltinFunctions.Min, 
                        new IExpression[]{new ColumnExp(_table.Columns[0])}), "min(A)")
            });
        result.GetRows().Should().BeEquivalentTo(
            new []{new Row(7), new Row(8), new Row(7)},
            cfg => cfg.WithStrictOrdering());
    }
}