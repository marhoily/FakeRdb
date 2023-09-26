namespace FakeRdb.Tests;

public sealed class TableTests
{
    private readonly Table _table = new("T", new[]
    {
        new ColumnHeader(0, "X", "T.X", TypeAffinity.NotSet),
        new ColumnHeader(1, "Y", "T.Y", TypeAffinity.NotSet)
    });

    public TableTests()
    {
        _table.Add(7, "a");
        _table.Add(8, "b");
        _table.Add(7, "c");
    }

    [Fact]
    public void GroupBy()
    {

    }

}