namespace FakeRdb.Tests;

public sealed class TypeAffinityTests : ComparisonTestBase
{
    public TypeAffinityTests(ITestOutputHelper output) : base(output)
    {
        Sqlite.SeedColumnAffinityTable();
        Sut.SeedColumnAffinityTable();
    }
    [Fact]
    public void TypeOf_Insert_Integer_As_Text()
    {
        CompareAgainstSqlite(
            """
            -- Values stored as TEXT, INTEGER, INTEGER, REAL, TEXT.
            INSERT INTO t1 VALUES('500.0', '500.0', '500.0', '500.0', '500.0');
            """);
        CompareAgainstSqlite(
            """
            SELECT typeof(t), typeof(nu), typeof(i), typeof(r), typeof(no) FROM t1;
            -- text|integer|integer|real|text
            """);
    }
    [Fact]
    public void VisibleTypes_Of_Insert_Integer_As_Text()
    {
        CompareAgainstSqlite(
            """
            -- Values stored as TEXT, INTEGER, INTEGER, REAL, TEXT.
            INSERT INTO t1 VALUES('500.0', '500.0', '500.0', '500.0', '500.0');
            """, printOut: false);
        CompareAgainstSqlite(
            """
            SELECT * FROM t1;
            """);
    }
}