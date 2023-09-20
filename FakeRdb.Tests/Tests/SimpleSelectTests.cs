using Xunit.Abstractions;

namespace FakeRdb.Tests;

public sealed class SimpleSelectTests : ComparisonTests
{
    public SimpleSelectTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.Seed3Albums();
        Sut.Seed3Albums();
    }

    [Fact]
    public void Table_Not_Found()
    {
        CompareAgainstSqlite("SELECT * FROM Wrong");
    }

    [Fact]
    public void Select_EveryColumn()
    {
        CompareAgainstSqlite("SELECT * FROM Album");
    }
    [Fact]
    public void Select_OneColumn()
    {
        CompareAgainstSqlite("SELECT Title FROM Album");
    }
    [Fact]
    public void Select_Wrong_Column()
    {
        CompareAgainstSqlite("SELECT Wrong FROM Album");
    }
    [Fact]
    public void Select_CaseSensitive_Column()
    {
        CompareAgainstSqlite("SELECT title FROM Album");
    }
    [Fact]
    public void Select_Escaped_Column()
    {
        CompareAgainstSqlite("SELECT [title] FROM Album");
    }
    [Fact]
    public void Select_Escaped_Table()
    {
        CompareAgainstSqlite("SELECT `title` FROM `Album`");
    }
    [Fact]
    public void ColumnName_In_RValue()
    {
        CompareAgainstSqlite("select year+ 1 from Album");
    }
    [Fact]
    public void Expr_In_Select()
    {
        CompareAgainstSqlite("select 1 +1 from Album");
    }
}