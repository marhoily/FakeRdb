namespace FakeRdb.Tests;

public sealed class DataSeederTests
{
    private readonly FakeDb _fakeDb = new();
    private readonly FakeDbConnection _connection;

    public DataSeederTests()
    {
        _connection = new FakeDbConnection(_fakeDb);
    }

    [Fact]
    public void Seed3Albums_Should_Create_Correct_Schema()
    {
        _connection.Seed3Albums();
        _fakeDb["Album"].Schema.Should().BeEquivalentTo(new[]
        {
            new Field("Id", typeof(long), true),
            new Field("Title", typeof(string)),
            new Field("Artist", typeof(string)),
            new Field("Year", typeof(long)),
        });

    }
    [Fact]
    public void Seed3Albums_Should_Populate_Correct_Data()
    {
        _connection.Seed3Albums();

        _fakeDb["Album"].Should().BeEquivalentTo(new[]
        {
            new Row(_fakeDb["Album"], new object[] { 1, "Track 1", "Artist 1", 2021L }),
            new Row(_fakeDb["Album"], new object[] { 2, "Track 2", "Artist 2", 2022L }),
            new Row(_fakeDb["Album"], new object[] { 3, "Track 3", "Artist 3", 2023L }),
        });
    }

}