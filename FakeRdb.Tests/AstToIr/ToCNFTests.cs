using System.Linq.Expressions;
using Antlr4.Runtime;
using static FakeRdb.IR;

namespace FakeRdb.Tests;

public sealed class ToCnfTests
{
    private static readonly bool A = default;
    private static readonly bool B = default;
    private static readonly bool C = default;
    private static readonly bool D = default;
    
    // ReSharper disable DoubleNegationOperator
    [Fact] public void Simplest() => AssertCnf(() => A,"A");
    [Fact] public void And() => AssertCnf(() => A && B,"A && B");
    [Fact] public void AndOfOr() => AssertCnf(() => (A || B) && C,"(A || B) && C");
    [Fact] public void OrOfAnd() => AssertCnf(() => (A && B) || C,"(A || C) && (B || C)");
    [Fact] public void NotOfAnd() => AssertCnf(() => !(A && B), "!A || !B");
    [Fact] public void NotOfOr() => AssertCnf(() => !(A || B), "!A && !B");
    [Fact] public void DoubleNegative() => AssertCnf(() => !!A, "A");
    [Fact] public void AndOrCombo() => AssertCnf(() =>
        (A && B) || (C && D), 
        "(A || C) && (A || D) && (B || C) && (B || D)");
    [Fact] public void ComplexExpression1() => AssertCnf(() => 
        (A && (B || C)) || D, 
        "(A || D) && (B || C || D)");
    [Fact] public void ComplexExpression2() => AssertCnf(() => 
        !(A && (B || C)) || D, 
        "(!A || !B) && (!A || !C) || D");
    // ReSharper restore DoubleNegationOperator

    public static void AssertCnf(Expression<Func<bool>> exp, string expected)
    {
        Exp(exp)
            .ToCnf().Print().Should()
            .Be(expected);
    }
    public static IExpression Exp(Expression<Func<bool>> exp)
    {
        return ConvertToExpression(exp.Body);


        static IExpression ConvertToExpression(Expression exp)
        {
            switch (exp)
            {
                case UnaryExpression unaryExpression:
                    return new UnaryExp(ConvertToUnaryOperator(unaryExpression.NodeType),
                        ConvertToExpression(unaryExpression.Operand));
                case BinaryExpression binaryExp:
                    return new BinaryExp(ConvertToBinaryOperator(binaryExp.NodeType),
                        ConvertToExpression(binaryExp.Left), ConvertToExpression(binaryExp.Right));
                case MemberExpression memberExp:
                    // Assumes that the Member is a simple property/field access
                    return new ColumnExp(memberExp.Member.Name);
                case ConstantExpression constExp:
                    return new LiteralExp(constExp.Value?.ToString() ?? "NULL");
                default:
                    throw new NotSupportedException($"Expression type {exp.GetType().Name} is not supported.");
            }
        }


        static UnaryOperator ConvertToUnaryOperator(ExpressionType expType)
        {
            return expType switch
            {
                ExpressionType.Not => UnaryOperator.Not,
                _ => throw new NotSupportedException($"ExpressionType {expType} is not supported.")
            };
        }
        static BinaryOperator ConvertToBinaryOperator(ExpressionType expType)
        {
            return expType switch
            {
                ExpressionType.AndAlso => BinaryOperator.And,
                ExpressionType.OrElse => BinaryOperator.Or,
                ExpressionType.Equal => BinaryOperator.Equal,
                _ => throw new NotSupportedException($"ExpressionType {expType} is not supported.")
            };
        }
    }

    public static IExpression Execute(string expression)
    {
        var inputStream = new AntlrInputStream(expression);
        var lexer = new SQLiteLexer(inputStream);
        var tokens = new CommonTokenStream(lexer);
        var parser = new SQLiteParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new AntlrExceptionThrowingErrorListener());
        var context = parser.expr();
        var visitor = new AstToIrVisitor("", null!, null!);
        return (IExpression)visitor.Visit(context)!;
    }
}