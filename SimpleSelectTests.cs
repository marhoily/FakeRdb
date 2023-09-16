using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace FakeRdb
{
    public sealed class SimpleSelectTests
    {
        private readonly FakeDb _db = new();

        [Fact]
        public void Table_Not_Found()
        {
            Assert.Throws<KeyNotFoundException>(
                () => _db.Execute("""
                                 SELECT *
                                 FROM tracks
                                 """)).Message.Should()
                .Be("The given key 'tracks' was not present in the dictionary.");
        }
        [Fact]
        public void Select_EveryColumn()
        {
            _db["tracks"] = new Table
            {
                new object[] { 1 }
            };
            var result = _db.Execute(
                """
                SELECT *
                FROM tracks
                """);
            result.Should().BeEquivalentTo(
                new View
                {
                    new object[] { 1 }
                });
        }

        [Fact]
        public void FactMethodName()
        {
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            SetUp();

            // Act
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Title, Artist, Year " +
                              "FROM Album";
            using var reader = cmd.ExecuteReader();
            // Assert
            reader.ShouldEqual(new FakeDbReader(
                new QueryResult(new []{
                    new Field("Title", typeof(string)),
                    new Field("Artist", typeof(string)),
                    new Field("Year", typeof(long))
                }, new List<List<object?>>{ 
                    new() {"Track 1", "Artist 1", 2021},
                    new() {"Track 2", "Artist 2", 2022},
                    new() {"Track 3", "Artist 3", 2023},
                })));
            return;

            void InsertTracks(SqliteCommand insertRow, string title, string artist, int year)
            {
                insertRow.Parameters.AddWithValue("@Title", title);
                insertRow.Parameters.AddWithValue("@Artist", artist);
                insertRow.Parameters.AddWithValue("@Year", year);
                insertRow.ExecuteNonQuery();
                insertRow.Parameters.Clear();
            }

        
            void SetUp()
            {
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
            }

        }
    }
}