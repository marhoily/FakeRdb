using static FakeRdb.BinaryOperator;
using static FakeRdb.IR;

namespace FakeRdb;

public static class BooleanAlgebra
{
    public sealed record AndGroup(IExpression[] Conditions);
    public sealed record OrGroup(AndGroup[] Alternatives);

    public static OrGroup? DecomposeDnf(this IExpression expr)
    {
        switch (expr)
        {
            case BinaryExp { Operand: Or } binaryOr:
            {
                var leftGroup = DecomposeDnf(binaryOr.Left);
                var rightGroup = DecomposeDnf(binaryOr.Right);

                if (leftGroup is null || rightGroup is null)
                    return null;

                return new OrGroup(leftGroup.Alternatives.Concat(rightGroup.Alternatives).ToArray());
            }
            case BinaryExp { Operand: And } binaryAnd:
            {
                var andGroup = ExtractAndConditions(binaryAnd);
                return new OrGroup(new[] { andGroup });
            }
            default:
                // Single condition, wrapped in AndGroup and OrGroup
                return new OrGroup(new[] { new AndGroup(new[] { expr }) });
        }
    }

    private static AndGroup ExtractAndConditions(BinaryExp binaryAnd)
    {
        var conditions = new List<IExpression>();

        TraverseAnd(binaryAnd);
        return new AndGroup(conditions.ToArray());

        void TraverseAnd(BinaryExp exp)
        {
            if (exp.Left is BinaryExp { Operand: And } leftAnd)
                TraverseAnd(leftAnd);
            else
                conditions.Add(exp.Left);

            if (exp.Right is BinaryExp { Operand: And } rightAnd)
                TraverseAnd(rightAnd);
            else
                conditions.Add(exp.Right);
        }
    }

    /// <summary>
    /// Transforms the given expression into its Conjunctive Normal Form (CNF).
    /// The CNF form of an expression is an AND of ORs.
    /// </summary>
    /// <param name="expr">The expression to be transformed.</param>
    /// <returns>The CNF representation of the given expression.</returns>
    /// <remarks>
    /// For example, OR on top:
    /// <code>(A and B) or (C and D)</code>
    /// gets transformed into:
    /// <code>(A or C) and (A or D) and (B or C) and (B or D) </code>
    /// </remarks>
    public static IExpression ToCnf(this IExpression expr)
    {
        var afterDeMorgans = ApplyDeMorgansLaw(expr);
        var cnf = DistributeOrOverAnd(afterDeMorgans);
        return SimplifyIdempotent(cnf);
    }
    
    /// <summary>
    /// Transforms the given expression into its Disjunctive Normal Form (DNF).
    /// The DNF form of an expression is an OR of ANDs.
    /// </summary>
    /// <param name="expr">The expression to be transformed.</param>
    /// <returns>The DNF representation of the given expression.</returns>
    /// <remarks>
    /// For example, AND on top:
    /// <code>(A or B) and (C or D)</code>
    /// gets transformed into:
    /// <code>(A and C) or (A and D) or (B and C) or (B and D) </code>
    /// </remarks>
    public static IExpression ToDnf(this IExpression expr)
    {
        var afterDeMorgans = ApplyDeMorgansLaw(expr);
        var dnf = DistributeAndOverOr(afterDeMorgans);
        return SimplifyIdempotent(dnf);
    }

    private static bool AreEquivalent(IExpression x, IExpression y)
    {
        return (x, y) switch
        {
            (LiteralExp a, LiteralExp b) => a.Value == b.Value,
            (ColumnExp a, ColumnExp b) => a.FullColumnName == b.FullColumnName,
            (UnaryExp a, UnaryExp b) => a.Op == b.Op && AreEquivalent(a.Operand, b.Operand),
            (BinaryExp a, BinaryExp b) => a.Operand == b.Operand && AreEquivalent(a.Left, b.Left) &&
                                          AreEquivalent(a.Right, b.Right),
            (BindExp a, BindExp b) => Equals(a.Value, b.Value),
            (AggregateExp a, AggregateExp b) => a.Function == b.Function &&
                                                a.Args.Zip(b.Args, AreEquivalent).All(equivalent => equivalent),
            (ScalarExp a, ScalarExp b) => a.Function == b.Function &&
                                          a.Args.Zip(b.Args, AreEquivalent).All(equivalent => equivalent),
            (InExp a, InExp b) => AreEquivalent(a.Needle, b.Needle) && a.Haystack == b.Haystack,
            _ => false
        };
    }

    private static IExpression SimplifyIdempotent(IExpression expr)
    {
        // Base case: If the expression is a leaf node, return as is
        if (expr is not BinaryExp binaryExp)
            return expr;

        // Recursive case: First, simplify the children
        var simplifiedLeft = SimplifyIdempotent(binaryExp.Left);
        var simplifiedRight = SimplifyIdempotent(binaryExp.Right);

        // Then, check for idempotence
        if ((binaryExp.Operand == And || binaryExp.Operand == Or) && AreEquivalent(simplifiedLeft, simplifiedRight))
        {
            return simplifiedLeft;
        }

        // If no idempotent simplification was possible, return a simplified version of the original expression
        return new BinaryExp(binaryExp.Operand, simplifiedLeft, simplifiedRight);
    }

    private static IExpression ApplyDeMorgansLaw(IExpression exp)
    {
        return exp switch
        {
            UnaryExp { Op: UnaryOperator.Not, Operand: BinaryExp { Operand: And, Left: var left, Right: var right } } 
                => new BinaryExp(Or, ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, left)), ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, right))),

            UnaryExp { Op: UnaryOperator.Not, Operand: BinaryExp { Operand: Or, Left: var left, Right: var right } } 
                => new BinaryExp(And, ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, left)), ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, right))),

            UnaryExp { Op: UnaryOperator.Not, Operand: UnaryExp { Op: UnaryOperator.Not, Operand: var inner } } 
                => ApplyDeMorgansLaw(inner),

            UnaryExp { Op: UnaryOperator.Not, Operand: var inner } 
                => new UnaryExp(UnaryOperator.Not, ApplyDeMorgansLaw(inner)),

            BinaryExp { Operand: And, Left: var left, Right: var right } 
                => new BinaryExp(And, ApplyDeMorgansLaw(left), ApplyDeMorgansLaw(right)),

            BinaryExp { Operand: Or, Left: var left, Right: var right } 
                => new BinaryExp(Or, ApplyDeMorgansLaw(left), ApplyDeMorgansLaw(right)),

            _ => exp
        };
    }

    private static IExpression DistributeOrOverAnd(IExpression expr)
    {
        return expr switch
        {
            BinaryExp
                {
                    Operand: Or,
                    Left: BinaryExp { Operand: And, Left: var leftAndLeft, Right: var leftAndRight },
                    Right: var right
                } =>
                // (A AND B) OR C -> (A OR C) AND (B OR C)
                new BinaryExp(And,
                    DistributeOrOverAnd(new BinaryExp(Or, leftAndLeft, right)),
                    DistributeOrOverAnd(new BinaryExp(Or, leftAndRight, right))),
            BinaryExp
                {
                    Operand: Or,
                    Left: var left,
                    Right: BinaryExp { Operand: And, Left: var rightAndLeft, Right: var rightAndRight }
                } =>
                // A OR (B AND C) -> (A OR B) AND (A OR C)
                new BinaryExp(And,
                    DistributeOrOverAnd(new BinaryExp(Or, left, rightAndLeft)),
                    DistributeOrOverAnd(new BinaryExp(Or, left, rightAndRight))),
            BinaryExp binaryExp =>
                // If neither child is an AND, recurse on the children and return
                new BinaryExp(binaryExp.Operand, DistributeOrOverAnd(binaryExp.Left), DistributeOrOverAnd(binaryExp.Right)),
            _ => expr
        };
    }

    private static IExpression DistributeAndOverOr(IExpression expr)
    {
        return expr switch
        {
            BinaryExp
                {
                    Operand: And,
                    Left: BinaryExp { Operand: Or, Left: var leftOrLeft, Right: var leftOrRight },
                    Right: var right
                } =>
                // (A OR B) AND C -> (A AND C) OR (B AND C)
                new BinaryExp(Or, DistributeAndOverOr(new BinaryExp(And, leftOrLeft, right)),
                    DistributeAndOverOr(new BinaryExp(And, leftOrRight, right))),
            BinaryExp
                {
                    Operand: And,
                    Left: var left,
                    Right: BinaryExp { Operand: Or, Left: var rightOrLeft, Right: var rightOrRight }
                } =>
                // A AND (B OR C) -> (A AND B) OR (A AND C)
                new BinaryExp(Or, DistributeAndOverOr(new BinaryExp(And, left, rightOrLeft)),
                    DistributeAndOverOr(new BinaryExp(And, left, rightOrRight))),
            BinaryExp binaryExp =>
                // If neither child is an OR, recurse on the children and return
                new BinaryExp(binaryExp.Operand, DistributeAndOverOr(binaryExp.Left), DistributeAndOverOr(binaryExp.Right)),
            _ => expr
        };
    }
}