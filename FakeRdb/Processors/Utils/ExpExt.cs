using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace FakeRdb;

public static class ExpExt
{
    public static T Resolve<T>(this IExpression exp, Row[] dataSet) => (T)exp.Eval(dataSet)!;
    public static T Resolve<T>(this IExpression exp, Row dataSet) => (T)exp.Eval(dataSet)!;
    public static T Resolve<T>(this IExpression exp) => (T)exp.Eval()!;

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
            SQLiteLexer.PIPE2 => new BinaryExpression(Operator.Concatenation, left, right, exp),
            SQLiteLexer.STAR => new BinaryExpression(Operator.Multiplication, left, right, exp),
            SQLiteLexer.DIV => new BinaryExpression(Operator.Division, left, right, exp),
            SQLiteLexer.MOD => new BinaryExpression(Operator.Modulus, left, right, exp),
            SQLiteLexer.PLUS => new BinaryExpression(Operator.Addition, left, right, exp),
            SQLiteLexer.MINUS => new BinaryExpression(Operator.Subtraction, left, right, exp),
            SQLiteLexer.LT2 => new BinaryExpression(Operator.BinaryLeftShift, left, right, exp),
            SQLiteLexer.GT2 => new BinaryExpression(Operator.BinaryRightShift, left, right, exp),
            SQLiteLexer.AMP => new BinaryExpression(Operator.BinaryAnd, left, right, exp),
            SQLiteLexer.PIPE => new BinaryExpression(Operator.BinaryOr, left, right, exp),
            SQLiteLexer.LT => new BinaryExpression(Operator.Less, left, right, exp),
            SQLiteLexer.LT_EQ => new BinaryExpression(Operator.LessOrEqual, left, right, exp),
            SQLiteLexer.GT => new BinaryExpression(Operator.Greater, left, right, exp),
            SQLiteLexer.GT_EQ => new BinaryExpression(Operator.GreaterOrEqual, left, right, exp),
            SQLiteLexer.ASSIGN => new BinaryExpression(Operator.Equal, left, right, exp),
            SQLiteLexer.EQ => new BinaryExpression(Operator.Equal, left, right, exp),
            SQLiteLexer.NOT_EQ1 => new BinaryExpression(Operator.NotEqual, left, right, exp),
            SQLiteLexer.NOT_EQ2 => new BinaryExpression(Operator.NotEqual, left, right, exp),
            SQLiteLexer.IS_ when context.children[2] is ITerminalNode { Symbol.Type: SQLiteLexer.NOT_ } =>
                new BinaryExpression(Operator.IsNot, left, right, exp),
            SQLiteLexer.IS_ => new BinaryExpression(Operator.Is, left, right, exp),
            SQLiteLexer.IN_ => new BinaryExpression(Operator.In, left, right, exp),
            SQLiteLexer.LIKE_ => new BinaryExpression(Operator.Like, left, right, exp),
            SQLiteLexer.GLOB_ => new BinaryExpression(Operator.Glob, left, right, exp),
            SQLiteLexer.MATCH_ => new BinaryExpression(Operator.Match, left, right, exp),
            SQLiteLexer.REGEXP_ => new BinaryExpression(Operator.RegExp, left, right, exp),
            SQLiteLexer.AND_ => new BinaryExpression(Operator.And, left, right, exp),
            SQLiteLexer.OR_ => new BinaryExpression(Operator.Or, left, right, exp),
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType))
        };
    }
}