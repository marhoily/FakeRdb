using Antlr4.Runtime.Tree;
using static FakeRdb.IR;
using static SQLiteParser;

namespace FakeRdb;

/// <summary>
/// Walks the ANTLR parse tree to perform the following simplifications:
/// <list type="bullet">
///     <item>Unroll "*" in SELECT</item>
///     <item>Resolve table, column, and function names</item>
///     <item>Dereference aliases</item>
///     <item>Unquote and parse literals</item>
///     <item>Bind SQL parameters</item>
///     <item>Simplify constant expressions</item>
///     <item>Push single-table predicates closer to their tables</item>
///     <item>Recognize equi-join predicates</item>
/// TODO:
///     <item>Check function call argument types</item>
/// 
/// </list>
/// </summary>
public sealed class AstToIrVisitor : SQLiteParserBaseVisitor<IResult?>
{
    // ReSharper disable once NotAccessedPositionalProperty.Local
    private sealed record Join(Table Left, JoinOperator Op, Table Right, IExpression Constraint) : IR;

    private readonly string _originalSql;
    private readonly Database _db;
    private readonly FakeDbParameterCollection _parameters;
    private ScopedValue<bool> _explainQueryPlan;
    private ScopedValue<Dictionary<string, Table>> _currentTables;
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
            (Table t, Affected a) => new QueryResult(t, a.RecordsCount),
            (Affected x, Affected y) => new Affected(x.RecordsCount + y.RecordsCount),

            _ => throw new ArgumentOutOfRangeException(nameof(aggregate))
        };
    }

    public override IResult? VisitSql_stmt(Sql_stmtContext context)
    {
        using var _ = _explainQueryPlan.Set(
            context.EXPLAIN_() != null && 
            context.QUERY_() != null);

        return VisitChildren(context);
    }

    public override IResult? VisitCreate_table_stmt(Create_table_stmtContext context)
    {
        var tableName = context.table_name().GetText();
        var columns = context.column_def().Select((col, n) =>
                new ColumnHeader(n,
                    col.column_name().GetText(),
                    tableName + "." + col.column_name().GetText(),
                    col.type_name().ToRuntimeType(),
                    col.column_constraint().Any(c => c.AUTOINCREMENT_() != null)))
            .ToArray();
        _db.Add(tableName, new Table(tableName, columns));
        return null;
    }

    public override IResult VisitInsert_stmt(Insert_stmtContext context)
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

    public override IResult VisitValues_clause(Values_clauseContext context)
    {
        return new ValuesTable(context.value_row()
            .Select(r => new ValuesRow(r.expr()
                .Select(Visit<IExpression>)
                .ToArray()))
            .ToArray());
    }

    public override IResult VisitSelect_stmt(Select_stmtContext context)
    {
        var select = Visit<ICompoundSelect>(context.select_expr());
        var orderBy = TryVisit<OrderBy>(context.order_by_stmt());
        var stmt = orderBy != null
            ? new SelectStmt(select, orderBy.Terms)
            : new SelectStmt(select);
        return _db.ExecuteStmt(stmt, _explainQueryPlan.Value).ResolveColumnTypes();
    }

    public override IResult VisitSelect_expr(Select_exprContext context)
    {
        var right = Visit<ICompoundSelect>(context.select_core());
        var left = TryVisit<ICompoundSelect>(context.select_expr());

        if (left == null) return right;

        // Assuming the operator is always the second child
        var op = context.ToCompoundOperator(
            context.GetChild(1).GetText());
        return new CompoundSelect(op, left, right);
    }

    public override IResult VisitSelect_core(Select_coreContext context)
    {
        using var a = _alias.OpenScope();
        using var t = _currentTables.Set(new Dictionary<string, Table>());
        var tables = context.table_or_subquery()
            .Select(Visit<Table>)
            .ToList();
        var select = context.result_column()
            .SelectMany(c => Visit<ResultColumnList>(c).List)
            .ToArray();
        var filter = TryVisit<IExpression>(context.whereExpr);
        var groupBy = context._groupByExpr
            .Select(c => Visit<ColumnExp>(c).FullColumnName)
            .ToArray();
        // EquiJoins and NonEquiJoins
        var join = TryVisit<Join>(context.join_clause());
        if (join != null)
        {
            filter = Expr.And(filter, join.Constraint);
            tables.Add(join.Left);
            tables.Add(join.Right);
        }


        var alternativeSources = ConditionAnalyzer
            .BuildAlternativeSources(tables, filter);
        return new SelectCore(alternativeSources, select, groupBy);
    }

    public override IResult VisitUpdate_stmt(Update_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        var table = _db[tableName];
        using var _ = _currentTables.Open();
        _currentTables.Value.Add(tableName, table);
        var assignments = context.update_assignment()
            .Select(a => (
                ColumnName: tableName + "." + a.column_name().GetText(),
                Value: Visit<IExpression>(a.expr())))
            .ToArray();
        var filter = TryVisit<IExpression>(context.where_clause()?.expr());
        var recordsAffected = _db.Update(tableName, assignments, filter);
        return new Affected(recordsAffected);
    }

    public override IResult VisitOrder_by_stmt(Order_by_stmtContext context)
    {
        var columnExp = Visit<ColumnExp>(context.ordering_term().Single());
        return new OrderBy(new[] { new OrderingTerm(columnExp.FullColumnName) });
    }

    public override IResult? VisitExpr(ExprContext context)
    {
        if (context.BIND_PARAMETER() is { } bind)
        {
            var exp = bind.GetText();
            var value = _parameters[exp].Value.CoerceToStoredType();
            return new BindExp(value);
        }

        // try and filter out binary\unary expression
        if (context.children[0] is not ExprContext)
            return VisitChildren(context);
        if (context.children[1] is not ITerminalNode { Symbol.Type: var operand })
            return VisitChildren(context);

        var left = TryVisit<IExpression>(context.children.First()) ?? throw new NotImplementedException();
        var right = Visit(context.children.Last()) ?? throw new NotImplementedException();
        if (operand == SQLiteLexer.IN_)
        {
            var table = (Table)right;
            return new InExp(left, table.Columns.Single());
        }

        var op = context.ToBinaryOperator(operand);
        return new BinaryExp(op, left, (IExpression)right);
    }

    public override IResult VisitLiteral_value(Literal_valueContext context)
    {
        return new LiteralExp(context.GetText());
    }

    public override IResult VisitResult_column(Result_columnContext context)
    {
        if (context.STAR() != null)
        {
            return new ResultColumnList(
                _currentTables.Value.Values.SelectMany(t => t.Columns
                    .Select(col => new ResultColumn(
                        new ColumnExp(t, col.Header.FullName), "*")))
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

    public override IResult VisitColumn_access(Column_accessContext context)
    {
        return ResolveColumn(ResolveTables()) ??
               throw Resources.ColumnNotFound(context.GetText());

        IResult? ResolveColumn(ICollection<Table>? tables)
        {
            if (tables == null) return null;

            var columnName = context.column_name().GetText().Unescape();
            var candidates = tables
                .Select(t => (table: t, column: t.TryLocal(columnName)))
                .Where(p => p.column != null)
                .ToArray();
            return candidates switch
            {
                // It's not allowed to access aliases declared in SELECT while still in select
                [] when _alias.TryGet(columnName, out var exp) => exp,
                [] => null,
                [var c] => new ColumnExp(c.table, c.column!.Header.FullName),
                _ => throw Resources.AmbiguousColumnReference(columnName)
            };
        }
        ICollection<Table>? ResolveTables()
        {
            var tableRef = context.table_name()?.GetText();
            var table = tableRef != null && _currentTables.Value
                .TryGetValue(tableRef, out var tbl)
                ? tbl
                : _db.Try(tableRef);
            if (table == null && tableRef != null)
                return null;
            return table != null
                ? new[] { table }
                : _currentTables.Value.Values;
        }
    }

    public override IResult VisitFunction_call(Function_callContext context)
    {
        var functionName = context.function_name().GetText()!;
        return functionName.ToFunctionCall(context.expr()
            .Select(Visit<IExpression>)
            .ToArray());
    }

    public override IResult VisitDelete_stmt(Delete_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        using var _ = _currentTables.Open();
        _currentTables.Value[tableName] = _db[tableName];
        return new Affected(_db.Delete(tableName,
            TryVisit<IExpression>(context.expr())));
    }

    public override IResult VisitTable_or_subquery(Table_or_subqueryContext context)
    {
        var tableName = context.table_name().GetText().Unescape();
        var alias = context.table_alias()?.GetText();

        var table = _db[tableName];
        _currentTables.Value.Add(alias ?? tableName, table);
        return table;
    }

    public override IResult VisitJoin_clause(Join_clauseContext context)
    {
        var left = Visit<Table>(context.table_or_subquery(0));
        var right = Visit<Table>(context.table_or_subquery(1));
        var constraint = Visit<IExpression>(context.join_constraint(0));
        var op = context.join_operator(0).ToJoinOperator();
        return new Join(left, op, right, constraint);
    }
    
    public override IResult VisitJoin_constraint(Join_constraintContext context)
    {
        return Visit<IExpression>(context.expr());
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