namespace FakeRdb.Tests;

public sealed class SimpleSelectTests : ComparisonTests
{
    [Fact]
    public void Table_Not_Found()
    {
        AssertReadersMatch("SELECT * FROM Album");
    }

    [Fact]
    public void Select_EveryColumn()
    {
        Prototype.Seed3Albums();
        Sut.Seed3Albums();
        AssertReadersMatch("SELECT * FROM Album");
    }
    [Fact]
    public void Select_OneColumn()
    {
        Prototype.Seed3Albums();
        Sut.Seed3Albums();
        AssertReadersMatch("SELECT Title FROM Album");
    }
    [Fact]
    public void Select_Wrong_Column()
    {
        Prototype.Seed3Albums();
        Sut.Seed3Albums();
        AssertReadersMatch("SELECT Wrong FROM Album");
    }
}