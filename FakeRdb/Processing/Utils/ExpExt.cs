using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace FakeRdb;

public static class ExpExt
{
    public static T Eval<T>(this IExpression exp, Row[] dataSet) => (T)exp.Eval(dataSet)!;
    public static T Eval<T>(this IExpression exp, Row dataSet) => (T)exp.Eval(dataSet)!;
    public static T Eval<T>(this IExpression exp) => (T)exp.Eval()!;
    public static T Eval<T>(this IR.IExpression exp, Row[] dataSet) => (T)exp.Eval(dataSet)!;
    public static T Eval<T>(this IR.IExpression exp, Row dataSet) => (T)exp.Eval(dataSet)!;
    public static T Eval<T>(this IR.IExpression exp) => (T)exp.Eval()!;

    public static string GetOriginalText(this ParserRuleContext context, string original)
    {
        int start = context.Start.StartIndex;
        int stop = context.Stop.StopIndex;

        if (start >= 0 && stop >= 0)
        {
            return original.Substring(start, stop - start + 1);
        }

        return "";
    }
    public static BinaryExpression ToBinaryExpression(this SQLiteParser.ExprContext context, int tokenType, IExpression left, IExpression right, string exp)
    {
        return tokenType switch
        {
            SQLiteLexer.PIPE2 => new BinaryExpression(Operator.Concatenation, left, right),
            SQLiteLexer.STAR => new BinaryExpression(Operator.Multiplication, left, right),
            SQLiteLexer.DIV => new BinaryExpression(Operator.Division, left, right),
            SQLiteLexer.MOD => new BinaryExpression(Operator.Modulus, left, right),
            SQLiteLexer.PLUS => new BinaryExpression(Operator.Addition, left, right),
            SQLiteLexer.MINUS => new BinaryExpression(Operator.Subtraction, left, right),
            SQLiteLexer.LT2 => new BinaryExpression(Operator.BinaryLeftShift, left, right),
            SQLiteLexer.GT2 => new BinaryExpression(Operator.BinaryRightShift, left, right),
            SQLiteLexer.AMP => new BinaryExpression(Operator.BinaryAnd, left, right),
            SQLiteLexer.PIPE => new BinaryExpression(Operator.BinaryOr, left, right),
            SQLiteLexer.LT => new BinaryExpression(Operator.Less, left, right),
            SQLiteLexer.LT_EQ => new BinaryExpression(Operator.LessOrEqual, left, right),
            SQLiteLexer.GT => new BinaryExpression(Operator.Greater, left, right),
            SQLiteLexer.GT_EQ => new BinaryExpression(Operator.GreaterOrEqual, left, right),
            SQLiteLexer.ASSIGN => new BinaryExpression(Operator.Equal, left, right),
            SQLiteLexer.EQ => new BinaryExpression(Operator.Equal, left, right),
            SQLiteLexer.NOT_EQ1 => new BinaryExpression(Operator.NotEqual, left, right),
            SQLiteLexer.NOT_EQ2 => new BinaryExpression(Operator.NotEqual, left, right),
            SQLiteLexer.IS_ when context.children[2] is ITerminalNode { Symbol.Type: SQLiteLexer.NOT_ } =>
                new BinaryExpression(Operator.IsNot, left, right),
            SQLiteLexer.IS_ => new BinaryExpression(Operator.Is, left, right),
            SQLiteLexer.IN_ => new BinaryExpression(Operator.In, left, right),
            SQLiteLexer.LIKE_ => new BinaryExpression(Operator.Like, left, right),
            SQLiteLexer.GLOB_ => new BinaryExpression(Operator.Glob, left, right),
            SQLiteLexer.MATCH_ => new BinaryExpression(Operator.Match, left, right),
            SQLiteLexer.REGEXP_ => new BinaryExpression(Operator.RegExp, left, right),
            SQLiteLexer.AND_ => new BinaryExpression(Operator.And, left, right),
            SQLiteLexer.OR_ => new BinaryExpression(Operator.Or, left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType))
        };
    }
    public static Operator ToBinaryOperator(this SQLiteParser.ExprContext context, int tokenType)
    {
        return tokenType switch
        {
            SQLiteLexer.PIPE2 => Operator.Concatenation,
            SQLiteLexer.STAR => Operator.Multiplication,
            SQLiteLexer.DIV => Operator.Division,
            SQLiteLexer.MOD => Operator.Modulus,
            SQLiteLexer.PLUS => Operator.Addition,
            SQLiteLexer.MINUS => Operator.Subtraction,
            SQLiteLexer.LT2 => Operator.BinaryLeftShift,
            SQLiteLexer.GT2 => Operator.BinaryRightShift,
            SQLiteLexer.AMP => Operator.BinaryAnd,
            SQLiteLexer.PIPE => Operator.BinaryOr,
            SQLiteLexer.LT => Operator.Less,
            SQLiteLexer.LT_EQ => Operator.LessOrEqual,
            SQLiteLexer.GT => Operator.Greater,
            SQLiteLexer.GT_EQ => Operator.GreaterOrEqual,
            SQLiteLexer.ASSIGN => Operator.Equal,
            SQLiteLexer.EQ => Operator.Equal,
            SQLiteLexer.NOT_EQ1 => Operator.NotEqual,
            SQLiteLexer.NOT_EQ2 => Operator.NotEqual,
            SQLiteLexer.IS_ when context.children[2] is ITerminalNode { Symbol.Type: SQLiteLexer.NOT_ } =>
                Operator.IsNot,
            SQLiteLexer.IS_ => Operator.Is,
            SQLiteLexer.IN_ => Operator.In,
            SQLiteLexer.LIKE_ => Operator.Like,
            SQLiteLexer.GLOB_ => Operator.Glob,
            SQLiteLexer.MATCH_ => Operator.Match,
            SQLiteLexer.REGEXP_ => Operator.RegExp,
            SQLiteLexer.AND_ => Operator.And,
            SQLiteLexer.OR_ => Operator.Or,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType))
        };
    }
}