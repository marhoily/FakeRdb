namespace FakeRdb;

public sealed class ReaderVisitor : SQLiteParserBaseVisitor<FakeDbReader>
{
    private readonly FakeDb _db;
    private FakeDbReader? _defaultResult;

    protected override FakeDbReader DefaultResult => _defaultResult!;

    public ReaderVisitor(FakeDb db)
    {
        _db = db;
    }

    public override FakeDbReader VisitSelect_core(SQLiteParser.Select_coreContext context)
    {
        var tableName = context.table_or_subquery().Single().table_name().GetText().Unescape();
        var dbTable = _db[tableName];
        var dbSchema = dbTable.Schema;
        var arr = context.result_column();

        var selectedIndices = arr.Length == 1 && arr[0].GetText() == "*"
            ? Enumerable.Range(0, dbSchema.Columns.Length).ToArray()
            : arr.Select(col => dbSchema.IndexOf(col.GetColumnName())).ToArray();
        if (selectedIndices.Length == 0) 
            throw new InvalidOperationException($"No columns selected from table: {tableName}");
        var table = dbTable
            .Select(dbRow => selectedIndices
                .Select(column => dbRow.Data[column])
                .ToList())
            .ToList();
        var schema = selectedIndices
            .Select(column => dbSchema.Columns[column])
            .ToArray();
        return _defaultResult = new FakeDbReader(
            new QueryResult(schema, table));
    }

}