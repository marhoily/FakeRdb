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
            _db["tracks"] = new Table(Array.Empty<Field>())
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
            var sql = "SELECT Year FROM Album";

            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            connection.Seed3Albums();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();

            _db["Album"] = new Table(
                new[] { new Field("Year", typeof(long)) })
            {
                new object[] { 2021L },
                new object[] { 2022L },
                new object[] { 2023L },
            };
            var result = _db.ExecuteReader(sql);
            reader.ShouldEqual(result);
        }

    }
}