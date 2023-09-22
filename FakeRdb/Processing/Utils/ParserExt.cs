using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace FakeRdb;

public static class ParserExt
{
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