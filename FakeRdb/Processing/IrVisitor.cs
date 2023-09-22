using Antlr4.Runtime.Tree;

namespace FakeRdb;

public sealed class IrVisitor : SQLiteParserBaseVisitor<IResult?>
{
    private readonly string _originalSql;
    private readonly Database _db;
    private readonly FakeDbParameterCollection _parameters;
    private Scoped<Table> _currentTable;
    private Scoped<Dictionary<string, IR.IExpression>> _alias;

    public IrVisitor(string originalSql, Database db, FakeDbParameterCollection parameters)
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

    public override IResult? VisitSql_stmt(SQLiteParser.Sql_stmtContext context)
    {
        using var _ = _alias.Set(new Dictionary<string, IR.IExpression>());
        return VisitChildren(context);
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
                .Cast<IR.IExpression>()
                .Select(X.Convert)
                .ToArray()))
            .ToArray());
    }

    public override IResult VisitSelect_stmt(SQLiteParser.Select_stmtContext context)
    {
        var select = (IR.SelectCore)Visit(context.select_core().Single())!;
        if (context.order_by_stmt() is { } orderByStmt)
        {
            var orderBy = (IR.OrderBy)Visit(orderByStmt)!;
            return IR.Execute(_db, new IR.SelectStmt(new[] { select }, orderBy.Terms));
        }

        return IR.Execute(_db, new IR.SelectStmt(
            new[] { select }, 
            Array.Empty<IR.OrderingTerm>()));
    }

    public override IResult VisitSelect_core(SQLiteParser.Select_coreContext context)
    {
        var tableName = context.table_or_subquery()
            .Single()
            .table_name()
            .GetText()
            .Unescape();
        using var _ = _currentTable.Set(_db[tableName]);
        var select = context.result_column()
            .Select(Visit)
            .Cast<IR.ResultColumnList>()
            .SelectMany(l => l.List)
            .ToArray();
        var filter = context.whereExpr == null ? null : Visit(context.whereExpr);
        return new IR.SelectCore(tableName, select, (IR.IExpression?)filter);

    }

    public override IResult VisitUpdate_stmt(SQLiteParser.Update_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        var table = _db[tableName];
        using var _ = _currentTable.Set(table);
        var assignments = context.update_assignment()
            .Select(a => (
                ColumnName: a.column_name().GetText(),
                Value: ((IR.IExpression)Visit(a.expr())!).Convert()))
            .ToArray();
        var where = context.where_clause()?.expr();
        var filter = where == null ? null : ((IR.IExpression)Visit(where)!).Convert();
        var recordsAffected = _db.Update(tableName, assignments, filter);
        return new Affected(recordsAffected);
    }

    public override IResult VisitOrder_by_stmt(SQLiteParser.Order_by_stmtContext context)
    {
        var orderingTerm = Visit(context.ordering_term().Single())!;
        var columnExp = (IR.ColumnExp)orderingTerm;
        return new IR.OrderBy(new[] { new IR.OrderingTerm(columnExp.Value) });
    }

    public override IResult? VisitExpr(SQLiteParser.ExprContext context)
    {
        if (context.BIND_PARAMETER() is { } bind)
        {
            var exp = bind.GetText();
            var value = _parameters[exp].Value;
            return new IR.BindExp(exp, value);
        }

        // try and filter out binary\unary expression
        if (context.children[0] is not SQLiteParser.ExprContext) 
            return VisitChildren(context);
        if (context.children[1] is not ITerminalNode { Symbol.Type: var operand }) 
            return VisitChildren(context);

        var left = (IR.IExpression)(Visit(context.children.First()) ?? throw new NotImplementedException());
        var right = Visit(context.children.Last()) ?? throw new NotImplementedException();
        if (operand == SQLiteLexer.IN_)
        {
            return new IR.InExp(left, (QueryResult)right);
        }

        return new IR.BinaryExp(context.ToBinaryOperator(operand), 
            left, (IR.IExpression)right, context.GetOriginalText(_originalSql));
        //return context.ToBinaryExpression(operand, left,
        //    (IExpression)right, context.GetOriginalText(_originalSql));
    }

    public override IResult VisitLiteral_value(SQLiteParser.Literal_valueContext context)
    {
        //return new ValueExpression(context.GetText());
        return new IR.LiteralExp(context.GetText());
    }

    public override IResult VisitResult_column(SQLiteParser.Result_columnContext context)
    {
        if (context.STAR() != null)
        {
            return new IR.ResultColumnList(
                _currentTable.Value.Schema.Columns.Select(field =>
                        new IR.ResultColumn(new IR.ColumnExp(field)))
                    .ToArray());
        }
        var result = (IR.IExpression?)Visit(context.expr()) ?? throw new Exception();
        if (context.column_alias() is { } alias)
        {
            var aliasText = alias.GetText().Unquote();
            _alias.Value.Add(aliasText, result);
           // result.SetAlias(aliasText);
        }

        return new IR.ResultColumnList(
            new IR.ResultColumn(result, 
                context.column_alias()?.GetText().Unquote()));
    }

    public override IResult VisitColumn_access(SQLiteParser.Column_accessContext context)
    {
        var tableName = context.table_name()?.GetText();
        var table = _db.Try(tableName) ?? _currentTable.Value;
        if (table == null)
            throw new InvalidOperationException("Couldn't resolve table!");

        var columnRef = context.column_name().GetText().Unescape();
        var column = table.Schema.TryGet(columnRef);
        if (column == null)
        {
            if (_alias.Value.TryGetValue(columnRef, out var exp))
                return exp;
            throw SchemaOperations.FieldNotFound(columnRef);
        }

        return new IR.ColumnExp(column);
    }

    public override IResult VisitFunction_call(SQLiteParser.Function_callContext context)
    {
        var functionName = context.function_name().GetText()!;
        var args = context.expr()
            .Select(Visit)
            .Cast<IR.IExpression>()
            .ToArray();
        return new IR.CallExp(functionName, args);
    }

    public override IResult VisitDelete_stmt(SQLiteParser.Delete_stmtContext context)
    {
        var tableName = context.qualified_table_name().GetText();
        _currentTable.Set(_db[tableName]);
        return new Affected(_db.Delete(tableName, GetPredicate()));
        IExpression? GetPredicate()
        {
            if (context.expr() is {} where)
                return ((IR.IExpression)Visit(where)!).Convert();
            return null;
        }
    }
}