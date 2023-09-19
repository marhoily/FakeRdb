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
        CompareAgainstSqlite("SELECT * FROM Album");
    }

    [Fact]
    public void Select_EveryColumn()
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
        CompareAgainstSqlite("SELECT * FROM Album");
    }
    [Fact]
    public void Select_OneColumn()
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
        CompareAgainstSqlite("SELECT Title FROM Album");
    }
    [Fact]
    public void Select_Wrong_Column()
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
        CompareAgainstSqlite("SELECT Wrong FROM Album");
    }
    [Fact]
    public void Select_CaseSensitive_Column()
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
        CompareAgainstSqlite("SELECT title FROM Album");
    }
    [Fact]
    public void Select_Escaped_Column()
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
        CompareAgainstSqlite("SELECT [title] FROM Album");
    }
    [Fact]
    public void Select_Escaped_Table()
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
        CompareAgainstSqlite("SELECT `title` FROM `Album`");
    }
}