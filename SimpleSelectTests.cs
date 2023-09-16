using System.Data.Common;
using Dapper;
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
            var sql = "SELECT * FROM Album";

            using var connection = 
                new SqliteConnection("Data Source=:memory:")
                    .Seed3Albums();
           
            using var con2 = new FakeDbConnection(_db)
                .Seed3Albums();
           
            Compare.Readers(connection, con2, sql);
        }
    }

    public static class Compare
    {
        public static void Readers(
            DbConnection c1, 
            DbConnection c2, 
            string sql)
        {
            var cmd1 = c1.CreateCommand();
            cmd1.CommandText = sql;
            var reader = cmd1.ExecuteReader();

            var cmd2 = c2.CreateCommand();
            cmd2.CommandText = sql;
            var result = cmd2.ExecuteReader();

            reader.ShouldEqual(result);
        }
    }
}