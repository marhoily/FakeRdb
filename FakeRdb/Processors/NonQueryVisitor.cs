namespace FakeRdb;

public sealed class NonQueryVisitor : SQLiteParserBaseVisitor<int>
{
    private readonly FakeDb _db;
    private readonly FakeDbParameterCollection _parameters;

    public NonQueryVisitor(FakeDb db, FakeDbParameterCollection parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public override int VisitInsert_stmt(SQLiteParser.Insert_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var table = _db[tableName];
        if (context.values_clause() is not { } values) return base.VisitInsert_stmt(context);
        var sqlRows = values.value_row();
        var valueSelectors = table.Schema
            .Select(field =>
            {
                var idx = Array.FindIndex(context.column_name(), col => col.GetText() == field.Name);
                if (idx != -1)
                {
                    return rowIndex => sqlRows[rowIndex].expr(idx).Resolve(_parameters);
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

    public override int VisitCreate_table_stmt(SQLiteParser.Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var fields = context.column_def().Select(col =>
                new Field(col.column_name().GetText(),
                    col.type_name().ToRuntimeType(),
                    col.column_constraint().Any(c => c.AUTOINCREMENT_() != null)))
            .ToArray();
        _db.Add(tableName, new Table(fields));
        return base.VisitCreate_table_stmt(context);
    }
}