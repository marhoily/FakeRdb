namespace FakeRdb.Tests;

public sealed class OrderByTests : ComparisonTestBase
{
    public OrderByTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData("Order by", "SELECT * FROM T ORDER BY X")]
    [InlineData("Order by", "SELECT * FROM T ORDER BY Y")]
    [InlineData("Order by", "SELECT * FROM T ORDER BY Z")]
    public void F(string d, string sql)
    {
        Execute("""
                CREATE TABLE T (X,Y,Z);
                INSERT INTO T (X, Y, Z) 
                VALUES 
                    ('Banana', 5, '2022-01-10'),
                    (12345, 'Apple', x'4D414E47'),   -- x'4D414E47' is a BLOB literal for 'MANG'
                    ('Orange', 8.23, 'This is text.'),
                    (98765, NULL, 999),
                    (3.1415, 'Another text', x'ABCDE12345');
                """);
        CompareAgainstSqlite(sql, d);
    }

}