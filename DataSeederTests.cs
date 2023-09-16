using FluentAssertions;

namespace FakeRdb;

public sealed class DataSeederTests 
{
    private readonly FakeDb _fakeDb = new();
    private readonly FakeDbConnection _connection;

    public DataSeederTests()
    {
        _connection = new FakeDbConnection(_fakeDb);
    }

    [Fact]
    public void SeedWith3Albums_Should_Create_Correct_Schema()
    {
        _connection.SeedWith3Albums();
        _fakeDb["Album"].Schema.Should().BeEquivalentTo(new[]
        {
            new Field("Id", typeof(int)),
            new Field("Title", typeof(string)),
            new Field("Artist", typeof(string)),
            new Field("Year", typeof(int)),
        });
         
    }
    [Fact]
    public void SeedWith3Albums_Should_Populate_Correct_Data()
    {
        _connection.SeedWith3Albums();

        _fakeDb["Album"].Should().BeEquivalentTo(new[]
        {
            new Row(_fakeDb["Album"], new object[] { 0, "Track 1", "Artist 1", 2021L }),
            new Row(_fakeDb["Album"], new object[] { 0, "Track 2", "Artist 2", 2022L }),
            new Row(_fakeDb["Album"], new object[] { 0, "Track 3", "Artist 3", 2023L }),
        });
    }

}