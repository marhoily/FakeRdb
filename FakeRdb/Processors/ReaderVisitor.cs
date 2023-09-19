namespace FakeRdb;

public sealed class ReaderVisitor : SQLiteParserBaseVisitor<FakeDbReader>
{
    private readonly FakeDb _db;
    private readonly FakeDbParameterCollection _parameters;
    private FakeDbReader? _defaultResult;

    protected override FakeDbReader DefaultResult => _defaultResult!;

    public ReaderVisitor(FakeDb db,  FakeDbParameterCollection parameters)
    {
        _db = db;
        _parameters = parameters;
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
    public override FakeDbReader VisitInsert_stmt(SQLiteParser.Insert_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var table = _db[tableName];
        if (context.values_clause() is not { } values) return base.VisitInsert_stmt(context);
        var sqlRows = values.value_row();
        var valueSelectors = table.Schema.Columns
            .Select(field =>
            {
                var idx = Array.FindIndex(context.column_name(), col => col.GetText() == field.Name);
                if (idx != -1)
                {
                    return rowIndex => sqlRows[rowIndex].expr(idx).Resolve(_parameters, field.FieldType);
                }
                if (field.IsAutoincrement)
                    return new Func<int, object?>(_ => table.Autoincrement());

                return _ => Activator.CreateInstance(field.FieldType);
            })
            .ToArray();

        for (var i = 0; i < sqlRows.Length; i++)
        {
            var oneRow = valueSelectors.Select(v => v(i)).ToArray();
            table.Add(oneRow);
        }
        return base.VisitInsert_stmt(context);
    }
    public override FakeDbReader VisitCreate_table_stmt(SQLiteParser.Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var fields = context.column_def().Select(col =>
                new Field(col.column_name().GetText(),
                    col.type_name().GetText(),
                    col.type_name().ToRuntimeType(),
                    col.column_constraint().Any(c => c.AUTOINCREMENT_() != null)))
            .ToArray();
        _db.Add(tableName, new Table(new TableSchema(fields)));
        return base.VisitCreate_table_stmt(context);
    }

}