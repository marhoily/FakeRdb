using System.Linq.Expressions;
using static FakeRdb.BinaryOperator;
using static FakeRdb.IR;
using static FakeRdb.UnaryOperator;

namespace FakeRdb.Tests;

public sealed class ToCnfDnfTests
{
    private static readonly bool A = default;
    private static readonly bool B = default;
    private static readonly bool C = default;
    private static readonly bool D = default;

    // ReSharper disable DoubleNegationOperator
    [Fact] public void Cnf_Simplest() => AssertCnf(() => A, "A");
    [Fact] public void Cnf_And() => AssertCnf(() => A && B, "A AND B");
    [Fact] public void Cnf_AndOfOr() => AssertCnf(() => (A || B) && C, "(A OR B) AND C");
    [Fact] public void Cnf_OrOfAnd() => AssertCnf(() => (A && B) || C, "(A OR C) AND (B OR C)");
    [Fact] public void Cnf_NotOfAnd() => AssertCnf(() => !(A && B), "!A OR !B");
    [Fact] public void Cnf_NotOfOr() => AssertCnf(() => !(A || B), "!A AND !B");
    [Fact] public void Cnf_DoubleNegative() => AssertCnf(() => !!A, "A");
    [Fact]
    public void Cnf_AndOrCombo() => AssertCnf(() =>
        (A && B) || (C && D),
        "(A OR C) AND (A OR D) AND (B OR C) AND (B OR D)");
    [Fact]
    public void Cnf_ComplexExpression1() => AssertCnf(() =>
        (A && (B || C)) || D,
        "(A OR D) AND (B OR C OR D)");
    [Fact]
    public void Cnf_ComplexExpression2() => AssertCnf(() =>
        !(A && (B || C)) || D,
        "(!A OR !B) AND (!A OR !C) OR D");

    [Fact] public void Dnf_Showcase() => AssertDnf(
        () => (A || B) && C, "A AND C OR B AND C");

    [Fact] public void Dnf_Simplest() => AssertDnf(() => A, "A");
    [Fact] public void Dnf_NotOfOr() => AssertDnf(() => !(A || B), "!A AND !B");
    [Fact]
    public void Dnf_AndOfOr() => AssertDnf(() =>
        (A || B) && (C || D),
        "A AND C OR A AND D OR B AND C OR B AND D");

    [Fact]
    public void Dnf_OrOfAnd() => AssertDnf(() =>
        (A && B) || (C && D), "A AND B OR C AND D");
    [Fact]
    public void Dnf_NestedAndOr() => AssertDnf(() =>
        (A || (B && C)) && D, "A AND D OR B AND C AND D");
    [Fact]
    public void Dnf_NestedOrAnd() => AssertDnf(() =>
        (A && (B || C)) || D, "A AND B OR A AND C OR D");

    // ReSharper restore DoubleNegationOperator

    private static void AssertCnf(Expression<Func<bool>> compTimeExp, string expectedExpression)
    {
        var (compiled, parsed, mapping) = Parse(compTimeExp);
        var cnf = parsed.ToCnf();
        Assert(expectedExpression, cnf, mapping, compiled, parsed);

    }
    private static void AssertDnf(Expression<Func<bool>> compTimeExp, string expectedExpression)
    {
        var (compiled, parsed, mapping) = Parse(compTimeExp);
        var dnf = parsed.ToDnf();
        Assert(expectedExpression, dnf, mapping, compiled, parsed);
    }

    /// <summary>
    /// Validates that an expression and its transformed representation (CNF\DNF) are semantically equivalent.
    /// This is done by:
    /// 1. Verifying that the transformation produces the expected expression.
    /// 2. Iterating through all possible permutations of parameter values and confirming that:
    ///    - The original expression, its transformed representation, and the C# counterpart all yield the same Boolean result.
    /// </summary>
    private static void Assert(string expectedExpression, 
        IExpression transformed, string[] mapping, 
        Func<uint, bool> compiled, IExpression parsed)
    {
        transformed.Print().Should().Be(expectedExpression);
        for (uint i = 0; i < (1 << mapping.Length); ++i)
        {
            var expectedCalc = compiled(i);
            Eval(parsed, mapping, i).Should().Be(expectedCalc);
            Eval(transformed, mapping, i).Should().Be(expectedCalc);
        }
    }

    private static bool Eval(IExpression exp, string[] mapping, uint bitfield)
    {
        return Inner(exp);
        bool Inner(IExpression a) =>
            a switch
            {
                BinaryExp { Operand: Or, Left: var left, Right: var right } =>
                    Inner(left) || Inner(right),
                BinaryExp { Operand: And, Left: var left, Right: var right } =>
                    Inner(left) && Inner(right),
                ColumnExp literal =>
                    (bitfield & (1 << Array.IndexOf(mapping, literal.FullColumnName))) != 0,
                UnaryExp { Op: Not, Operand: var operand } =>
                    !Inner(operand),
                _ => throw new ArgumentOutOfRangeException(nameof(a))
            };
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
                    return new ColumnExp(null!, name);
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
                ExpressionType.AndAlso => And,
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