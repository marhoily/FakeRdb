namespace FakeRdb;

public sealed class MyVisitor : SQLiteParserBaseVisitor<View>
{
    private readonly FakeDb _db;
    protected override View DefaultResult { get; } = new();

    public MyVisitor(FakeDb db)
    {
        _db = db;
    }

    public override View VisitTable_or_subquery(SQLiteParser.Table_or_subqueryContext context)
    {
        var tableNameContext = context.table_name().GetText();
        var table = _db[tableNameContext];
        DefaultResult.AddRange(table.Select(r=> r.Data));
        return DefaultResult;
        //return base.VisitTable_or_subquery(context);
    }
}
public sealed class ReaderVisitor : SQLiteParserBaseVisitor<FakeDbReader>
{
    private readonly FakeDb _db;
    private FakeDbReader? _defaultResult;

    protected override FakeDbReader DefaultResult => _defaultResult!;

    public ReaderVisitor(FakeDb db)
    {
        _db = db;
    }

    // public override Table VisitSelect_core(SQLiteParser.Select_coreContext context)
    // {
    //     Visit(context.table_or_subquery().Single());
    //     return base.VisitSelect_core(context);
    // }

    public override FakeDbReader VisitTable_or_subquery(SQLiteParser.Table_or_subqueryContext context)
    {
        var tableNameContext = context.table_name().GetText();
        Table table = _db[tableNameContext];
        _defaultResult = new FakeDbReader(
            new QueryResult(table.Schema, 
                table.Select(row => 
                    row.Data.Cast<object?>().ToList()).ToList()));
            
        return DefaultResult;
        //return base.VisitTable_or_subquery(context);
    }
}