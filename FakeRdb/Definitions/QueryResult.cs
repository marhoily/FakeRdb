namespace FakeRdb;

public sealed record QueryResult(Field[] Schema, List<List<object?>> Data);