using static SQLiteParser;

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
        var fields = context.column_def().Select(col =>
                new Field(col.column_name().GetText(),
                    col.type_name().ToRuntimeType()))
            .ToArray();
        _db.Add(tableName, new Table(fields));
        return base.VisitCreate_table_stmt(context);
    }
}

public static class TypeExt
{
    public static Type ToRuntimeType(this Type_nameContext context)
    {
        return context.GetText() switch
        {
            "TEXT" => typeof(string),
            "INTEGER" => typeof(int),
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}