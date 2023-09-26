namespace FakeRdb.Tests;

public sealed class TableTests
{
    private readonly Table _table = new(new[]
    {
        new ColumnHeader(0, "X", TypeAffinity.NotSet),
        new ColumnHeader(1, "Y", TypeAffinity.NotSet)
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