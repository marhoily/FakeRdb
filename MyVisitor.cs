namespace FakeRdb;

public sealed class MyVisitor : SQLiteParserBaseVisitor<View>
{
    private readonly FakeDb _db;
    protected override View DefaultResult { get; } = new();

    public MyVisitor(FakeDb db)
    {
        _db = db;
    }

    // public override Table VisitSelect_core(SQLiteParser.Select_coreContext context)
    // {
    //     Visit(context.table_or_subquery().Single());
    //     return base.VisitSelect_core(context);
    // }

    public override View VisitTable_or_subquery(SQLiteParser.Table_or_subqueryContext context)
    {
        var tableNameContext = context.table_name().GetText();
        var table = _db[tableNameContext];
        DefaultResult.AddRange(table.Select(r=> r.Data));
        return DefaultResult;
        //return base.VisitTable_or_subquery(context);
    }
}