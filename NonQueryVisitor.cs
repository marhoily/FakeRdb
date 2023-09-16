namespace FakeRdb;

public sealed class NonQueryVisitor : SQLiteParserBaseVisitor<int>
{
    private readonly FakeDb _db;

    public NonQueryVisitor(FakeDb db)
    {
        _db = db;
    }

    public override int VisitInsert_stmt(SQLiteParser.Insert_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var table = _db[tableName];
        var fields = context.column_name()
            .Select(c => table.GetColumn(c.GetText())).ToArray();
        if (context.values_clause() is not { } values) return base.VisitInsert_stmt(context);
        var sqlRow = values.value_row();
        for (var i = 0; i < sqlRow.Length; i++)
        {
            var dbRow = new object[table.Schema.Length];
            table.Add(dbRow);
        }
        return base.VisitInsert_stmt(context);
    }

    public override int VisitCreate_table_stmt(SQLiteParser.Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var fields = context.column_def().Select(col =>
                new Field(col.column_name().GetText(),
                    col.type_name().ToRuntimeType()))
            .ToArray();
        _db.Add(tableName, new Table(fields));
        return base.VisitCreate_table_stmt(context);
    }
}