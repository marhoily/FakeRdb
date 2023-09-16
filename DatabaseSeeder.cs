using System.Data.Common;

namespace FakeRdb;

public static class DatabaseSeeder
{
    public static DbConnection Seed3Albums(this DbConnection connection)
    {
        var factory = DbProviderFactories.GetFactory(connection) 
                      ?? throw new InvalidOperationException();
        connection.Open();
        using var createTable = connection.CreateCommand();
        createTable.CommandText =
            "CREATE TABLE Album (" +
            "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "Title TEXT, " +
            "Artist TEXT, " +
            "Year INTEGER)";
        createTable.ExecuteNonQuery();
           
        using var insertRow = connection.CreateCommand();
        insertRow.CommandText =
            "INSERT INTO Album (Title, Artist, Year) " +
            "VALUES (@Title, @Artist, @Year)";

        InsertTracks(insertRow, "Track 1", "Artist 1", 2021);
        InsertTracks(insertRow, "Track 2", "Artist 2", 2022);
        InsertTracks(insertRow, "Track 3", "Artist 3", 2023);

        return connection;

        void InsertTracks(DbCommand cmd, string title, string artist, int year)
        {
            cmd.SetParameter(factory, "@Title", title);
            cmd.SetParameter(factory, "@Artist", artist);
            cmd.SetParameter(factory, "@Year", year);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();
        }
    }
}