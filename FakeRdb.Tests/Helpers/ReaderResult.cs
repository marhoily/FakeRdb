namespace FakeRdb.Tests;

public sealed record ReaderResult(int RecordsAffected,
    List<(string ColumnType, string ColumnName)> Schema,
    List<List<object?>> Data);