namespace FakeRdb;

public sealed class SqlVisitor : SQLiteParserBaseVisitor<FakeDbReader>
{
    private readonly FakeDb _db;
    private readonly FakeDbParameterCollection _parameters;

    public SqlVisitor(FakeDb db, FakeDbParameterCollection parameters)
    {
        _db = db;
        _parameters = parameters;
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

    public override FakeDbReader VisitInsert_stmt(SQLiteParser.Insert_stmtContext context)
    {
        if (context.values_clause() is not { } values)
            throw new NotImplementedException();

        var tableName = context.table_name().GetText();
        var columns = context.column_name().Select(col => col.GetText()).ToArray();
        var rows = values.value_row();

        _db.Insert(tableName, columns, GetData, rows.Length);

        return base.VisitInsert_stmt(context);

        object? GetData(int rowIndex, int idx) =>
            rows[rowIndex].expr(idx).Resolve(_parameters);
    }

    public override FakeDbReader VisitSelect_core(SQLiteParser.Select_coreContext context)
    {
        var tableName = context.table_or_subquery()
            .Single()
            .table_name()
            .GetText()
            .Unescape();
        var projection = context.result_column()
            .Select(col => col.GetColumnName())
            .ToArray();
        return _db.Select(tableName, projection);
    }
}