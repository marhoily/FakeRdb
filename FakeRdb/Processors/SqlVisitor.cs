using Antlr4.Runtime.Tree;

namespace FakeRdb;

public sealed class SqlVisitor : SQLiteParserBaseVisitor<IResult?>
{
    private readonly FakeDb _db;
    private readonly FakeDbParameterCollection _parameters;
    private Scope<Table> _currentTable;

    public SqlVisitor(FakeDb db, FakeDbParameterCollection parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    protected override IResult? AggregateResult(IResult? aggregate, IResult? nextResult)
    {
        return nextResult ?? aggregate;
    }

    public override IResult? VisitCreate_table_stmt(SQLiteParser.Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var fields = context.column_def().Select((col, n) =>
                new Field(n,
                    col.column_name().GetText(),
                    col.type_name().GetText(),
                    col.type_name().ToRuntimeType(),
                    col.column_constraint().Any(c => c.AUTOINCREMENT_() != null)))
            .ToArray();
        _db.Add(tableName, new Table(new TableSchema(fields)));
        return null;
    }

    public override IResult? VisitInsert_stmt(SQLiteParser.Insert_stmtContext context)
    {
        var valuesTable = Visit(context.values_clause()) ?? throw new InvalidOperationException();
        var tableName = context.table_name().GetText();
        var columns = context.column_name().Select(col => col.GetText()).ToArray();
        _db.Insert(tableName, columns, (ValuesTable)valuesTable);
        return null;
    }

    public override IResult VisitValues_clause(SQLiteParser.Values_clauseContext context)
    {
        return new ValuesTable(context.value_row()
            .Select(r => new ValuesRow(r.expr()
                .Select(Visit)
                .Cast<Expression>()
                .ToArray()))
            .ToArray());
    }

    public override IResult VisitSelect_core(SQLiteParser.Select_coreContext context)
    {
        var tableName = context.table_or_subquery()
            .Single()
            .table_name()
            .GetText()
            .Unescape();
        using var _ = _currentTable.Set(_db[tableName]);
        var projection = context.result_column()
            .Select(col => col.GetColumnName())
            .ToArray();
        var filter = context.whereExpr == null ? null : Visit(context.whereExpr);
        return _db.Select(tableName, projection, (Expression?)filter);
    }

    public override IResult VisitUpdate_stmt(SQLiteParser.Update_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        var table = _db[tableName];
        using var _ = _currentTable.Set(table);
        var assignments = context.update_assignment()
            .Select(a => (
                ColumnName: a.column_name().GetText(),
                Value: (Expression)Visit(a.expr())!))
            .ToArray();
        var where = context.where_clause()?.expr();
        var filter = where == null ? null : (Expression)Visit(where)!;
        var recordsAffected = _db.Update(tableName, assignments, filter);
        return new Affected(recordsAffected);
    }

    public override IResult? VisitExpr(SQLiteParser.ExprContext context)
    {
        if (context.BIND_PARAMETER() is { } bind)
        {
            return new ValueExpression(_parameters[bind.GetText()].Value);
        }

        // try and filter out binary\unary expression
        if (context.children[0] is not SQLiteParser.ExprContext) return VisitChildren(context);
        if (context.children[1] is not ITerminalNode { Symbol.Type: var operand }) return VisitChildren(context);

        var left = (Expression)(Visit(context.children.First()) ?? throw new NotImplementedException());
        var right = Visit(context.children.Last()) ?? throw new NotImplementedException();
        if (operand == SQLiteLexer.IN_)
        {
            return new InExpression(left, (QueryResult)right);
        }

        return context.ToBinaryExpression(operand, left, (Expression)right);

    }

    public override IResult VisitLiteral_value(SQLiteParser.Literal_valueContext context)
    {
        return new ValueExpression(context.GetText().Unquote());
    }

    public override IResult VisitColumn_access(SQLiteParser.Column_accessContext context)
    {
        var table = _db.Try(context.table_name()?.GetText()) ??
                    _currentTable.Value;
        var column = context.column_name().GetText();
        if (table == null)
            throw new InvalidOperationException("Couldn't resolve table!");
        return new FieldAccessExpression(table.Schema[column]);
    }
}