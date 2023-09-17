using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class SimpleSelectTests : ComparisonTests
{
    public SimpleSelectTests(ITestOutputHelper output) : base(output)
    {
    }

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
    [Fact]
    public void Select_CaseSensitive_Column()
    {
        Prototype.Seed3Albums();
        Sut.Seed3Albums();
        AssertReadersMatch("SELECT title FROM Album");
    }
    [Fact]
    public void Select_Escaped_Column()
    {
        Prototype.Seed3Albums();
        Sut.Seed3Albums();
        AssertReadersMatch("SELECT [title] FROM Album");
    }
}