using Antlr4.Runtime.Tree;

namespace FakeRdb;

public static class ExpExt
{
    public static BinaryExpression ToBinaryExpression(this SQLiteParser.ExprContext context, int tokenType, Expression left, Expression right)
    {
        return tokenType switch
        {
            SQLiteLexer.PIPE2 => new BinaryExpression(Operator.Concatenation, left, right),
            SQLiteLexer.STAR => new BinaryExpression(Operator.Multiplication, left, right),
            SQLiteLexer.DIV => new BinaryExpression(Operator.Division, left, right),
            SQLiteLexer.MOD => new BinaryExpression(Operator.Modulus, left, right),
            SQLiteLexer.PLUS => new BinaryExpression(Operator.Addition, left, right),
            SQLiteLexer.MINUS => new BinaryExpression(Operator.Subtraction , left, right),
            SQLiteLexer.LT2 => new BinaryExpression(Operator.BinaryLeftShift, left, right),
            SQLiteLexer.GT2 => new BinaryExpression(Operator.BinaryRightShift, left, right),
            SQLiteLexer.AMP => new BinaryExpression(Operator.BinaryAnd, left, right),
            SQLiteLexer.PIPE => new BinaryExpression(Operator.BinaryOr, left, right),
            SQLiteLexer.LT => new BinaryExpression(Operator.Less, left, right),
            SQLiteLexer.LT_EQ => new BinaryExpression(Operator.LessOrEqual, left, right),
            SQLiteLexer.GT => new BinaryExpression(Operator.Greater, left, right),
            SQLiteLexer.GT_EQ=> new BinaryExpression(Operator.GreaterOrEqual, left, right),
            SQLiteLexer.ASSIGN=> new BinaryExpression(Operator.Equal, left, right),
            SQLiteLexer.EQ=> new BinaryExpression(Operator.Equal, left, right),
            SQLiteLexer.NOT_EQ1=> new BinaryExpression(Operator.NotEqual, left, right),
            SQLiteLexer.NOT_EQ2=> new BinaryExpression(Operator.NotEqual, left, right),
            SQLiteLexer.IS_ when context.children[2] is ITerminalNode { Symbol.Type: SQLiteLexer.NOT_ }=> 
                new BinaryExpression(Operator.IsNot, left, right),
            SQLiteLexer.IS_ => new BinaryExpression(Operator.Is, left, right),
            SQLiteLexer.IN_=> new BinaryExpression(Operator.In, left, right),
            SQLiteLexer.LIKE_=> new BinaryExpression(Operator.Like, left, right),
            SQLiteLexer.GLOB_=> new BinaryExpression(Operator.Glob, left, right),
            SQLiteLexer.MATCH_=> new BinaryExpression(Operator.Match, left, right),
            SQLiteLexer.REGEXP_=> new BinaryExpression(Operator.RegExp, left, right),
            SQLiteLexer.AND_=> new BinaryExpression(Operator.And, left, right),
            SQLiteLexer.OR_=> new BinaryExpression(Operator.Or, left, right),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}