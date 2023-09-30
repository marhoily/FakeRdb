namespace FakeRdb.Tests;

public static class DbDataReaderExtensions
{
    public static ReaderResult ToResult(this DbDataReader reader)
    {
        reader.IsClosed.Should().BeFalse();
        return new ReaderResult(reader.RecordsAffected,
            reader.GetSchema().ToList(), reader.ReadData());
    }
    public static IEnumerable<(string ColumnType, string ColumnName)> GetSchema(this DbDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            yield return (reader.GetDataTypeName(i), reader.GetName(i));
        }
    }

    public static List<List<object?>> ReadData(this DbDataReader reader)
    {
        var rows = new List<List<object?>>();
        while (reader.Read())
        {
            var row = new List<object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.GetValue(i);
                row.Add(value == DBNull.Value ? null : value);
            }
            rows.Add(row);
        }
        return rows;
    }
}