using Antlr4.Runtime.Tree;
using static FakeRdb.IR;

namespace FakeRdb;

public static class TokenToEnum
{
    public static BinaryOperator ToBinaryOperator(this SQLiteParser.ExprContext context, int tokenType)
    {
        return tokenType switch
        {
            SQLiteLexer.PIPE2 => BinaryOperator.Concatenation,
            SQLiteLexer.STAR => BinaryOperator.Multiplication,
            SQLiteLexer.DIV => BinaryOperator.Division,
            SQLiteLexer.MOD => BinaryOperator.Modulus,
            SQLiteLexer.PLUS => BinaryOperator.Addition,
            SQLiteLexer.MINUS => BinaryOperator.Subtraction,
            SQLiteLexer.LT2 => BinaryOperator.BinaryLeftShift,
            SQLiteLexer.GT2 => BinaryOperator.BinaryRightShift,
            SQLiteLexer.AMP => BinaryOperator.BinaryAnd,
            SQLiteLexer.PIPE => BinaryOperator.BinaryOr,
            SQLiteLexer.LT => BinaryOperator.Less,
            SQLiteLexer.LT_EQ => BinaryOperator.LessOrEqual,
            SQLiteLexer.GT => BinaryOperator.Greater,
            SQLiteLexer.GT_EQ => BinaryOperator.GreaterOrEqual,
            SQLiteLexer.ASSIGN => BinaryOperator.Equal,
            SQLiteLexer.EQ => BinaryOperator.Equal,
            SQLiteLexer.NOT_EQ1 => BinaryOperator.NotEqual,
            SQLiteLexer.NOT_EQ2 => BinaryOperator.NotEqual,
            SQLiteLexer.IS_ when context.children[2] is ITerminalNode { Symbol.Type: SQLiteLexer.NOT_ } =>
                BinaryOperator.IsNot,
            SQLiteLexer.IS_ => BinaryOperator.Is,
            SQLiteLexer.IN_ => BinaryOperator.In,
            SQLiteLexer.LIKE_ => BinaryOperator.Like,
            SQLiteLexer.GLOB_ => BinaryOperator.Glob,
            SQLiteLexer.MATCH_ => BinaryOperator.Match,
            SQLiteLexer.REGEXP_ => BinaryOperator.RegExp,
            SQLiteLexer.AND_ => BinaryOperator.And,
            SQLiteLexer.OR_ => BinaryOperator.Or,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType))
        };
    }

    public static CompoundOperator ToCompoundOperator(this SQLiteParser.Select_exprContext context, string operatorToken)
    {
        var compoundOperator = operatorToken switch
        {
            "ALL" => CompoundOperator.UnionAll,
            "UNION" => context.ALL_() == null
                ? CompoundOperator.Union
                : CompoundOperator.UnionAll,
            "EXCEPT" => CompoundOperator.Except,
            "INTERSECT" => CompoundOperator.Intersect,
            _ => throw new InvalidOperationException("WTF?")
        };
        return compoundOperator;
    }

    public static IResult ToFunctionCall(this string functionName, IExpression[] args)
    {
        return functionName.ToUpperInvariant() switch
        {
            "MAX" => new AggregateExp(SqliteBuiltinFunctions.Max, args),
            "MIN" => new AggregateExp(SqliteBuiltinFunctions.Min, args),
            "TYPEOF" => new ScalarExp(SqliteBuiltinFunctions.TypeOf, args),
            _ => throw new ArgumentOutOfRangeException(functionName)
        };
    }
}