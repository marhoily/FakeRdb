namespace FakeRdb;

public sealed class NonQueryVisitor : SQLiteParserBaseVisitor<int>
{
    private readonly FakeDb _db;

    public NonQueryVisitor(FakeDb db)
    {
        _db = db;
    }

    public override int VisitCreate_table_stmt(SQLiteParser.Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        _db.Add(tableName, new Table(Array.Empty<Field>()));
        return base.VisitCreate_table_stmt(context);
    }
}