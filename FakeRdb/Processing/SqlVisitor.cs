using Antlr4.Runtime.Tree;

namespace FakeRdb;

public sealed class SqlVisitor : SQLiteParserBaseVisitor<IResult?>
{
    private readonly string _originalSql;
    private readonly Database _db;
    private readonly FakeDbParameterCollection _parameters;
    private Scope<Table> _currentTable;

    public SqlVisitor(string originalSql, Database db, FakeDbParameterCollection parameters)
    {
        _originalSql = originalSql;
        _db = db;
        _parameters = parameters;
    }

    protected override IResult? AggregateResult(IResult? aggregate, IResult? nextResult)
    {
        return (nextResult, aggregate) switch{
            (null, null) => null,
            (var x, null) => x,
            (null, var y) => y,
            (QueryResult q, Affected a) => q.Merge(a),
            (Affected a, QueryResult q) => q.Merge(a),
            (Affected x, Affected y) => new Affected(x.RecordsCount + y.RecordsCount),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override IResult? VisitCreate_table_stmt(SQLiteParser.Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var fields = context.column_def().Select((col, n) =>
                new Field(n,
                    col.column_name().GetText(),
                    col.type_name().ToRuntimeType(),
                    col.column_constraint().Any(c => c.AUTOINCREMENT_() != null)))
            .ToArray();
        _db.Add(tableName, new Table(new TableSchema(fields)));
        return null;
    }

    public override IResult VisitInsert_stmt(SQLiteParser.Insert_stmtContext context)
    {
        /*
         * The right-hand operand of an IN or NOT IN operator has no
         * affinity if the operand is a list, or has the same affinity
         * as the affinity of the result set expression if the operand
         * is a SELECT. 
         */
        var valuesTable = Visit(context.values_clause()) ?? throw new InvalidOperationException();
        var tableName = context.table_name().GetText();
        var columns = context.column_name().Select(col => col.GetText()).ToArray();
        var values = (ValuesTable)valuesTable;
        _db.Insert(tableName, columns, values);
        return new Affected(values.Rows.Length);
    }

    public override IResult VisitValues_clause(SQLiteParser.Values_clauseContext context)
    {
        return new ValuesTable(context.value_row()
            .Select(r => new ValuesRow(r.expr()
                .Select(Visit)
                .Cast<IExpression>()
                .ToArray()))
            .ToArray());
    }

    public override IResult VisitSelect_stmt(SQLiteParser.Select_stmtContext context)
    {
        var select = (QueryResult)Visit(context.select_core().Single())!;
        if (context.order_by_stmt() is { } orderByStmt)
        {
            var orderBy = (OrderByClause)Visit(orderByStmt)!;
            select.Sort(orderBy);
        }

        return select.PostProcess();
    }

    public override IResult VisitSelect_core(SQLiteParser.Select_coreContext context)
    {
        var tableName = context.table_or_subquery()
            .Single()
            .table_name()
            .GetText()
            .Unescape();
        using var _ = _currentTable.Set(_db[tableName]);
        var select = context.result_column().Select(Visit).ToList();

        var aggregate = select
            .OfType<FunctionCallExpression>()
            .Where(f => f.IsAggregate)
            .ToList();
        if (aggregate.Count > 0)
            return _db.SelectAggregate(tableName, aggregate);
        var filter = context.whereExpr == null ? null : Visit(context.whereExpr);
        var projection = select.Cast<IProjection>().ToArray();
        var result = _db.Select(tableName, projection, (IExpression?)filter);
        return result;
    }

    public override IResult VisitUpdate_stmt(SQLiteParser.Update_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        var table = _db[tableName];
        using var _ = _currentTable.Set(table);
        var assignments = context.update_assignment()
            .Select(a => (
                ColumnName: a.column_name().GetText(),
                Value: (IExpression)Visit(a.expr())!))
            .ToArray();
        var where = context.where_clause()?.expr();
        var filter = where == null ? null : (IExpression)Visit(where)!;
        var recordsAffected = _db.Update(tableName, assignments, filter);
        return new Affected(recordsAffected);
    }

    public override IResult VisitOrder_by_stmt(SQLiteParser.Order_by_stmtContext context)
    {
        var orderingTerm = Visit(context.ordering_term().Single())!;
        var asProjection = (ProjectionExpression)orderingTerm;
        return new OrderByClause(asProjection.SelectColumn);
    }

    public override IResult? VisitExpr(SQLiteParser.ExprContext context)
    {
        if (context.BIND_PARAMETER() is { } bind)
        {
            var exp = bind.GetText();
            var value = _parameters[exp].Value;
            var affinity = value.GetTypeAffinity();
            return new ValueExpression(value, affinity, exp);
        }

        // try and filter out binary\unary expression
        if (context.children[0] is not SQLiteParser.ExprContext) return VisitChildren(context);
        if (context.children[1] is not ITerminalNode { Symbol.Type: var operand }) return VisitChildren(context);

        var left = (IExpression)(Visit(context.children.First()) ?? throw new NotImplementedException());
        var right = Visit(context.children.Last()) ?? throw new NotImplementedException();
        if (operand == SQLiteLexer.IN_)
        {
            return new InExpression(left, (QueryResult)right);
        }

        return context.ToBinaryExpression(operand, left,
            (IExpression)right, context.GetOriginalText(_originalSql));
    }

    public override IResult VisitLiteral_value(SQLiteParser.Literal_valueContext context)
    {
        return new ValueExpression(context.GetText());
    }

    public override IResult VisitResult_column(SQLiteParser.Result_columnContext context)
    {
        if (context.STAR() != null)
            return Wildcard.Instance;
        var result = (IExpression?)Visit(context.expr()) ?? throw new Exception();
        if (context.column_alias() is {} alias) 
            result.SetAlias(alias.GetText().Unquote());
        return result;
    }

    public override IResult VisitColumn_access(SQLiteParser.Column_accessContext context)
    {
        var table = _db.Try(context.table_name()?.GetText()) ??
                    _currentTable.Value;
        var column = context.column_name().GetText().Unescape();

        if (table == null)
            throw new InvalidOperationException("Couldn't resolve table!");
        return new ProjectionExpression(table.Schema.Get(column));
    }

    public override IResult VisitFunction_call(SQLiteParser.Function_callContext context)
    {
        var functionName = context.function_name().GetText()!;
        var args = context.expr().Select(Visit).Cast<IExpression>().ToArray();
        return new FunctionCallExpression(
            functionName,
            args);
    }

    public override IResult VisitDelete_stmt(SQLiteParser.Delete_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        return new Affected(_db.Delete(tableName));
    }
}