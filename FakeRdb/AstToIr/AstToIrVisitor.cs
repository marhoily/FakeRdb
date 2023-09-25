using Antlr4.Runtime.Tree;
using static FakeRdb.IR;

namespace FakeRdb;

/// <summary>
/// Walks the ANTLR parse tree to perform the following simplifications:
/// <list type="bullet">
///     <item>Unroll "*" in SELECT</item>
///     <item>Resolve table, column, and function names</item>
///     <item>Dereference aliases</item>
///     <item>Unquote and parse literals</item>
///     <item>Bind SQL parameters</item>
///     <item>Distinguish between plain and aggregate functions</item>
/// TODO:
///     <item>Check function call argument types</item>
/// 
/// </list>
/// </summary>
public sealed class AstToIrVisitor : SQLiteParserBaseVisitor<IResult?>
{
    private readonly string _originalSql;
    private readonly Database _db;
    private readonly FakeDbParameterCollection _parameters;
    private ScopedValue<Table[]> _currentTables;
    private readonly HierarchicalAliasStore<IExpression> _alias = new();

    public AstToIrVisitor(string originalSql, Database db, FakeDbParameterCollection parameters)
    {
        _originalSql = originalSql;
        _db = db;
        _parameters = parameters;
    }

    protected override IResult? AggregateResult(IResult? aggregate, IResult? nextResult)
    {
        return (nextResult, aggregate) switch
        {
            (null, null) => null,
            (var x, null) => x,
            (null, var y) => y,
            (QueryResult q, Affected a) => q.Merge(a),
            (Affected a, QueryResult q) => q.Merge(a),
            (Affected x, Affected y) => new Affected(x.RecordsCount + y.RecordsCount),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override IResult? VisitSql_stmt(SQLiteParser.Sql_stmtContext context)
    {
        return VisitChildren(context);
    }

    public override IResult? VisitCreate_table_stmt(SQLiteParser.Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var columns = context.column_def().Select((col, n) =>
                new ColumnHeader(n,
                    col.column_name().GetText(),
                    col.type_name().ToRuntimeType(),
                    col.column_constraint().Any(c => c.AUTOINCREMENT_() != null)))
            .ToArray();
        _db.Add(tableName, new Table(new TableSchema(columns)));
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
        var valuesTable = TryVisit<ValuesTable>(context.values_clause()) ??
                          throw new InvalidOperationException();
        var tableName = context.table_name().GetText();
        var columns = context.column_name()
            .Select(col => col.GetText())
            .ToArray();
        _db.Insert(tableName, columns, valuesTable);
        return new Affected(valuesTable.Rows.Length);
    }

    public override IResult VisitValues_clause(SQLiteParser.Values_clauseContext context)
    {
        return new ValuesTable(context.value_row()
            .Select(r => new ValuesRow(r.expr()
                .Select(Visit<IExpression>)
                .ToArray()))
            .ToArray());
    }

    public override IResult VisitSelect_stmt(SQLiteParser.Select_stmtContext context)
    {
        var select = Visit<ICompoundSelect>(context.select_expr());
        var orderBy = TryVisit<OrderBy>(context.order_by_stmt());
        var stmt = orderBy != null
            ? new SelectStmt(select, orderBy.Terms)
            : new SelectStmt(select);
        return _db.Execute(stmt).PostProcess();
    }

    public override IResult VisitSelect_expr(SQLiteParser.Select_exprContext context)
    {
        var right = Visit<ICompoundSelect>(context.select_core());
        var left = TryVisit<ICompoundSelect>(context.select_expr());

        if (left == null) return right;

        // Assuming the operator is always the second child
        var op = context.ToCompoundOperator(
            context.GetChild(1).GetText());
        return new CompoundSelect(op, left, right);
    }

    public override IResult VisitSelect_core(SQLiteParser.Select_coreContext context)
    {
        using var a = _alias.OpenScope();
        var tableNames = context.table_or_subquery()
            .Select(exp => exp.table_name().GetText().Unescape());
        var tables = tableNames.Select(name => _db[name]).ToArray();
        using var t = _currentTables.Set(tables);
        var select = context.result_column()
            .SelectMany(c => Visit<ResultColumnList>(c).List)
            .ToArray();
        var filter = TryVisit<IExpression>(context.whereExpr);
        var groupBy = context._groupByExpr
            .Select(c => Visit<ColumnExp>(c).Value)
            .ToArray();
        return new SelectCore(tables, select, groupBy, filter);
    }

    public override IResult VisitUpdate_stmt(SQLiteParser.Update_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        var table = _db[tableName];
        using var _ = _currentTables.Set(new [] {table});
        var assignments = context.update_assignment()
            .Select(a => (
                ColumnName: a.column_name().GetText(),
                Value: Visit<IExpression>(a.expr())))
            .ToArray();
        var filter = TryVisit<IExpression>(context.where_clause()?.expr());
        var recordsAffected = _db.Update(tableName, assignments, filter);
        return new Affected(recordsAffected);
    }

    public override IResult VisitOrder_by_stmt(SQLiteParser.Order_by_stmtContext context)
    {
        var columnExp = Visit<ColumnExp>(context.ordering_term().Single());
        return new OrderBy(new[] { new OrderingTerm(columnExp.Value) });
    }

    public override IResult? VisitExpr(SQLiteParser.ExprContext context)
    {
        if (context.BIND_PARAMETER() is { } bind)
        {
            var exp = bind.GetText();
            var value = _parameters[exp].Value;
            return new BindExp(value);
        }

        // try and filter out binary\unary expression
        if (context.children[0] is not SQLiteParser.ExprContext)
            return VisitChildren(context);
        if (context.children[1] is not ITerminalNode { Symbol.Type: var operand })
            return VisitChildren(context);

        var left = TryVisit<IExpression>(context.children.First()) ?? throw new NotImplementedException();
        var right = Visit(context.children.Last()) ?? throw new NotImplementedException();
        if (operand == SQLiteLexer.IN_)
        {
            return new InExp(left, (QueryResult)right);
        }

        var op = context.ToBinaryOperator(operand);
        return new BinaryExp(op, left, (IExpression)right);
    }

    public override IResult VisitLiteral_value(SQLiteParser.Literal_valueContext context)
    {
        return new LiteralExp(context.GetText());
    }

    public override IResult VisitResult_column(SQLiteParser.Result_columnContext context)
    {
        if (context.STAR() != null)
        {
            return new ResultColumnList(
                _currentTables.Value.SelectMany(t => t.Schema.Columns)
                    .Select(col => new ResultColumn(new ColumnExp(col), "*"))
                    .ToArray());
        }
        var result = TryVisit<IExpression>(context.expr()) ?? throw new Exception();
        var originalText = context.expr().GetOriginalText(_originalSql);
        if (context.column_alias() is { } alias)
        {
            var aliasText = alias.GetText().Unquote();
            _alias.Set(result, aliasText);
        }

        var als = context.column_alias()?.GetText().Unquote();
        return new ResultColumnList(
            new ResultColumn(result, originalText, als));
    }

    public override IResult VisitColumn_access(SQLiteParser.Column_accessContext context)
    {
        var tableName = context.table_name()?.GetText();
        var tables = _db.TrySet(tableName) ?? _currentTables.Value;
        if (tables == null)
            throw new InvalidOperationException("Couldn't resolve table!");

        var columnName = context.column_name().GetText().Unescape();
        var candidates = tables
            .Select(t => t.Schema.TryGet(columnName))
            .OfType<ColumnHeader>()
            .ToArray();
        return candidates switch
        {
            // It's not allowed to access aliases declared in SELECT while still in select
            [] when _alias.TryGet(columnName, out var exp) => exp,
            [] => throw Resources.ColumnNotFound(columnName),
            [var c] => new ColumnExp(c),
            _ => throw Resources.AmbiguousColumnReference(columnName)
        };
    }

    public override IResult VisitFunction_call(SQLiteParser.Function_callContext context)
    {
        var functionName = context.function_name().GetText()!;
        return functionName.ToFunctionCall(context.expr()
            .Select(Visit<IExpression>)
            .ToArray());
    }

    public override IResult VisitDelete_stmt(SQLiteParser.Delete_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        _currentTables.Set(new []{_db[tableName]});
        return new Affected(_db.Delete(tableName,
            TryVisit<IExpression>(context.expr())));
    }

    private T Visit<T>(IParseTree? tree)
    {
        var result = Visit(tree) ?? throw new InvalidOperationException();
        return (T)result;
    }
    private T? TryVisit<T>(IParseTree? tree)
    {
        if (tree == null) return default;
        return (T?)Visit(tree);
    }
}