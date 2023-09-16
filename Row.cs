namespace FakeRdb;

public sealed record Row(Table Table, object?[] Data);