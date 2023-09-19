using Antlr4.Runtime.Tree;

namespace FakeRdb;

public interface IResult { }

public sealed record Affected(int RecordsCount) : IResult;

public sealed class SqlVisitor : SQLiteParserBaseVisitor<IResult?>
{
    private readonly FakeDb _db;
    private readonly FakeDbParameterCollection _parameters;
    private Context<Table> _currentTable;

    public SqlVisitor(FakeDb db, FakeDbParameterCollection parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    protected override IResult? AggregateResult(IResult? aggregate, IResult? nextResult)
    {
        return aggregate ?? nextResult;
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
        if (context.values_clause() is not { } values)
            throw new NotImplementedException();

        var tableName = context.table_name().GetText();
        var columns = context.column_name().Select(col => col.GetText()).ToArray();
        var rows = values.value_row();

        _db.Insert(tableName, columns, GetData, rows.Length);

        return null;

        object? GetData(int rowIndex, int idx) =>
            rows[rowIndex].expr(idx).Resolve(_parameters);
    }

    public override IResult VisitSelect_core(SQLiteParser.Select_coreContext context)
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
        var filter = context.where_clause()?.expr().ToFilter(table);
        var recordsAffected = _db.Update(tableName, assignments, filter);
        return new Affected(recordsAffected);
    }

    public override IResult? VisitExpr(SQLiteParser.ExprContext context)
    {
        if (context.BIND_PARAMETER() is { } bind)
        {
            return new ValueExpression(_parameters[bind.GetText()].Value);
        }
        if (context.literal_value() is { } literal)
        {
            return new ValueExpression(literal.GetText().Unquote());
        }
        if (context.children[0] is SQLiteParser.ExprContext)
        {
            if (context.children[1] is ITerminalNode { Symbol.Type: var t })
            {
                var left = (Expression)(Visit(context.children[0]) ?? throw new NotImplementedException());
                var right = (Expression)(Visit(context.children[2]) ?? throw new NotImplementedException());
                return t switch
                {
                    SQLiteLexer.STAR => new BinaryExpression(left, Operator.Mul, right),
/*
 * expr PIPE2 expr
   | expr ( STAR | DIV | MOD) expr
   | expr ( PLUS | MINUS) expr
   | expr ( LT2 | GT2 | AMP | PIPE) expr
   | expr ( LT | LT_EQ | GT | GT_EQ) expr
   | expr (
   ASSIGN
   | EQ
   | NOT_EQ1
   | NOT_EQ2
   | IS_
   | IS_ NOT_
   | IN_
   | LIKE_
   | GLOB_
   | MATCH_
   | REGEXP_
   ) expr
   | expr AND_ expr
   | expr OR_ expr
 */
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        //throw new NotImplementedException(context.GetText());
        return VisitChildren(context);
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