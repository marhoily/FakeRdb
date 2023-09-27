using System.Linq.Expressions;
using static FakeRdb.BinaryOperator;
using static FakeRdb.IR;
using static FakeRdb.UnaryOperator;

namespace FakeRdb.Tests;

public sealed class ToCnfTests
{
    private static readonly bool A = default;
    private static readonly bool B = default;
    private static readonly bool C = default;
    private static readonly bool D = default;

    // ReSharper disable DoubleNegationOperator
    [Fact] public void Simplest() => AssertCnf(() => A, "A");
    [Fact] public void And() => AssertCnf(() => A && B, "A && B");
    [Fact] public void AndOfOr() => AssertCnf(() => (A || B) && C, "(A || B) && C");
    [Fact] public void OrOfAnd() => AssertCnf(() => (A && B) || C, "(A || C) && (B || C)");
    [Fact] public void NotOfAnd() => AssertCnf(() => !(A && B), "!A || !B");
    [Fact] public void NotOfOr() => AssertCnf(() => !(A || B), "!A && !B");
    [Fact] public void DoubleNegative() => AssertCnf(() => !!A, "A");
    [Fact]
    public void AndOrCombo() => AssertCnf(() =>
        (A && B) || (C && D),
        "(A || C) && (A || D) && (B || C) && (B || D)");
    [Fact]
    public void ComplexExpression1() => AssertCnf(() =>
        (A && (B || C)) || D,
        "(A || D) && (B || C || D)");
    [Fact]
    public void ComplexExpression2() => AssertCnf(() =>
        !(A && (B || C)) || D,
        "(!A || !B) && (!A || !C) || D");
    // ReSharper restore DoubleNegationOperator

    /// <summary>
    /// Validates that an expression and its Conjunctive Normal Form (CNF) representation are semantically equivalent.
    /// This is done by:
    /// 1. Verifying that the CNF transformation produces the expected expression.
    /// 2. Iterating through all possible permutations of parameter values and confirming that:
    ///    - The original expression, its CNF representation, and the C# counterpart all yield the same Boolean result.
    /// </summary>
    /// <param name="compTimeExp">The compile-time expression to test.</param>
    /// <param name="expectedExpression">The expected CNF expression as a string.</param>
    private static void AssertCnf(Expression<Func<bool>> compTimeExp, string expectedExpression)
    {
        var (compiled, parsed, mapping) = Parse(compTimeExp);
        var cnf = parsed.ToCnf();
        cnf.Print().Should().Be(expectedExpression);
        for (uint i = 0; i < (1 << mapping.Length); ++i)
        {
            var expectedCalc = compiled(i);
            Eval(parsed, i).Should().Be(expectedCalc);
            Eval(cnf, i).Should().Be(expectedCalc);
        }

        return;


        bool Eval(IExpression exp, uint bitfield)
        {
            return exp switch
            {
                BinaryExp { Op: Or, Left: var left, Right: var right } => 
                    Eval(left, bitfield) || Eval(right, bitfield),
                BinaryExp { Op: BinaryOperator.And, Left: var left, Right: var right } => 
                    Eval(left, bitfield) && Eval(right, bitfield),
                ColumnExp literal => 
                    (bitfield & (1 << Array.IndexOf(mapping,literal.FullColumnName))) != 0,
                UnaryExp { Op:Not, Operand: var operand } => 
                    !Eval(operand, bitfield),
                _ => throw new ArgumentOutOfRangeException(nameof(exp))
            };
        }
    }

    private static (Func<uint, bool>, IExpression, string[]) Parse(Expression<Func<bool>> exp)
    {
        var bitMapping = new List<string>();

        var convertedExp = ConvertToExpression(exp.Body);
        var func = CreateCompiledFunc(exp, bitMapping);

        return (func, convertedExp, bitMapping.ToArray());

        IExpression ConvertToExpression(Expression body)
        {
            switch (body)
            {
                case UnaryExpression unaryExpression:
                    return new UnaryExp(ConvertToUnaryOperator(unaryExpression.NodeType),
                        ConvertToExpression(unaryExpression.Operand));
                case BinaryExpression binaryExp:
                    return new BinaryExp(ConvertToBinaryOperator(binaryExp.NodeType),
                        ConvertToExpression(binaryExp.Left), ConvertToExpression(binaryExp.Right));
                case MemberExpression memberExp:
                    var name = memberExp.Member.Name;
                    if (!bitMapping.Contains(name))
                    {
                        bitMapping.Add(name);
                    }
                    return new ColumnExp(name);
                case ConstantExpression constExp:
                    return new LiteralExp(constExp.Value?.ToString() ?? "NULL");
                default:
                    throw new NotSupportedException($"Expression type {body.GetType().Name} is not supported.");
            }
        }

        static UnaryOperator ConvertToUnaryOperator(ExpressionType expType)
        {
            return expType switch
            {
                ExpressionType.Not => Not,
                _ => throw new NotSupportedException($"ExpressionType {expType} is not supported.")
            };
        }
        static BinaryOperator ConvertToBinaryOperator(ExpressionType expType)
        {
            return expType switch
            {
                ExpressionType.AndAlso => BinaryOperator.And,
                ExpressionType.OrElse => Or,
                ExpressionType.Equal => Equal,
                _ => throw new NotSupportedException($"ExpressionType {expType} is not supported.")
            };
        }
    }
    
    private static Func<uint, bool> CreateCompiledFunc(Expression<Func<bool>> originalExpression, List<string> bitMapping)
    {
        // Create parameter of type uint
        var param = Expression.Parameter(typeof(uint), "bits");

        // Method to convert the original expression tree into a new tree
        // that operates on the 'bits' parameter
        Expression ConvertToBits(Expression exp)
        {
            switch (exp)
            {
                case UnaryExpression unaryExp:
                    return Expression.MakeUnary(unaryExp.NodeType, ConvertToBits(unaryExp.Operand), unaryExp.Type);

                case BinaryExpression binaryExp:
                    return Expression.MakeBinary(binaryExp.NodeType,
                        ConvertToBits(binaryExp.Left), ConvertToBits(binaryExp.Right));

                case MemberExpression memberExp:
                    var position = bitMapping.IndexOf(memberExp.Member.Name);
                    if (position != -1)
                    {
                        // Access the bit at 'position' in 'bits'
                        // Create an expression equivalent to (bits & (1 << position)) != 0
                        var left = Expression.LeftShift(Expression.Constant(1U), Expression.Constant(position));
                        var and = Expression.And(param, left);
                        return Expression.NotEqual(and, Expression.Constant(0U));
                    }
                    throw new InvalidOperationException("Variable not found in bit mapping.");

                case ConstantExpression constExp:
                    return Expression.Constant(constExp.Value is true, typeof(bool));

                default:
                    throw new NotSupportedException($"Expression type {exp.GetType().Name} is not supported.");
            }
        }

        var body = ConvertToBits(originalExpression.Body);
        var lambda = Expression.Lambda<Func<uint, bool>>(body, param);

        return lambda.Compile();
    }
}