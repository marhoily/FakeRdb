namespace FakeRdb;

public static class DbDataReaderExtensions
{
    public static int CountRows(this DbDataReader reader)
    {
        var count = 0;
        while (reader.Read())
        {
            count++;
        }

        return count;
    }

}