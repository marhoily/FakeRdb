using static FakeRdb.IR;

namespace FakeRdb;

public static class BooleanAlgebra
{
    public static OrGroup? DecomposeDnf(this IExpression expr)
    {
        switch (expr)
        {
            case BinaryExp { Op: BinaryOperator.Or } binaryOr:
            {
                var leftGroup = DecomposeDnf(binaryOr.Left);
                var rightGroup = DecomposeDnf(binaryOr.Right);

                if (leftGroup is null || rightGroup is null)
                    return null;

                return new OrGroup(leftGroup.Alternatives.Concat(rightGroup.Alternatives).ToArray());
            }
            case BinaryExp { Op: BinaryOperator.And } binaryAnd:
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

        void TraverseAnd(BinaryExp exp)
        {
            if (exp.Left is BinaryExp leftAnd && leftAnd.Op == BinaryOperator.And)
                TraverseAnd(leftAnd);
            else
                conditions.Add(exp.Left);

            if (exp.Right is BinaryExp rightAnd && rightAnd.Op == BinaryOperator.And)
                TraverseAnd(rightAnd);
            else
                conditions.Add(exp.Right);
        }

        TraverseAnd(binaryAnd);
        return new AndGroup(conditions.ToArray());
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
        switch (x, y)
        {
            case (LiteralExp a, LiteralExp b):
                return a.Value == b.Value;

            case (ColumnExp a, ColumnExp b):
                return a.FullColumnName == b.FullColumnName;

            case (UnaryExp a, UnaryExp b):
                return a.Op == b.Op && AreEquivalent(a.Operand, b.Operand);

            case (BinaryExp a, BinaryExp b):
                return a.Op == b.Op && AreEquivalent(a.Left, b.Left) && AreEquivalent(a.Right, b.Right);

            case (BindExp a, BindExp b):
                return Equals(a.Value, b.Value);

            case (AggregateExp a, AggregateExp b):
                return a.Function == b.Function && a.Args.Zip(b.Args, AreEquivalent).All(equivalent => equivalent);

            case (ScalarExp a, ScalarExp b):
                return a.Function == b.Function && a.Args.Zip(b.Args, AreEquivalent).All(equivalent => equivalent);

            case (InExp a, InExp b):
                return AreEquivalent(a.Needle, b.Needle) && a.Haystack == b.Haystack;

            default:
                return false;
        }
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
        if ((binaryExp.Op == BinaryOperator.And || binaryExp.Op == BinaryOperator.Or) && AreEquivalent(simplifiedLeft, simplifiedRight))
        {
            return simplifiedLeft;
        }

        // If no idempotent simplification was possible, return a simplified version of the original expression
        return new BinaryExp(binaryExp.Op, simplifiedLeft, simplifiedRight);
    }

    private static IExpression ApplyDeMorgansLaw(IExpression exp)
    {
        return exp switch
        {
            UnaryExp { Op: UnaryOperator.Not, Operand: BinaryExp { Op: BinaryOperator.And, Left: var left, Right: var right } } 
                => new BinaryExp(BinaryOperator.Or, ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, left)), ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, right))),

            UnaryExp { Op: UnaryOperator.Not, Operand: BinaryExp { Op: BinaryOperator.Or, Left: var left, Right: var right } } 
                => new BinaryExp(BinaryOperator.And, ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, left)), ApplyDeMorgansLaw(new UnaryExp(UnaryOperator.Not, right))),

            UnaryExp { Op: UnaryOperator.Not, Operand: UnaryExp { Op: UnaryOperator.Not, Operand: var inner } } 
                => ApplyDeMorgansLaw(inner),

            UnaryExp { Op: UnaryOperator.Not, Operand: var inner } 
                => new UnaryExp(UnaryOperator.Not, ApplyDeMorgansLaw(inner)),

            BinaryExp { Op: BinaryOperator.And, Left: var left, Right: var right } 
                => new BinaryExp(BinaryOperator.And, ApplyDeMorgansLaw(left), ApplyDeMorgansLaw(right)),

            BinaryExp { Op: BinaryOperator.Or, Left: var left, Right: var right } 
                => new BinaryExp(BinaryOperator.Or, ApplyDeMorgansLaw(left), ApplyDeMorgansLaw(right)),

            _ => exp
        };
    }

    private static IExpression DistributeOrOverAnd(IExpression expr)
    {
        switch (expr)
        {
            case BinaryExp { Op: BinaryOperator.Or, Left: BinaryExp { Op: BinaryOperator.And, Left: var leftAndLeft, Right: var leftAndRight }, Right: var right }:
                // (A AND B) OR C -> (A OR C) AND (B OR C)
                return new BinaryExp(BinaryOperator.And, DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, leftAndLeft, right)), DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, leftAndRight, right)));

            case BinaryExp { Op: BinaryOperator.Or, Left: var left, Right: BinaryExp { Op: BinaryOperator.And, Left: var rightAndLeft, Right: var rightAndRight } }:
                // A OR (B AND C) -> (A OR B) AND (A OR C)
                return new BinaryExp(BinaryOperator.And, DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, left, rightAndLeft)), DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, left, rightAndRight)));
        
            case BinaryExp binaryExp:
                // If neither child is an AND, recurse on the children and return
                return new BinaryExp(binaryExp.Op, DistributeOrOverAnd(binaryExp.Left), DistributeOrOverAnd(binaryExp.Right));

            default:
                return expr;
        }
    }

    private static IExpression DistributeAndOverOr(IExpression expr)
    {
        switch (expr)
        {
            case BinaryExp { Op: BinaryOperator.And, Left: BinaryExp { Op: BinaryOperator.Or, Left: var leftOrLeft, Right: var leftOrRight }, Right: var right }:
                // (A OR B) AND C -> (A AND C) OR (B AND C)
                return new BinaryExp(BinaryOperator.Or, DistributeAndOverOr(new BinaryExp(BinaryOperator.And, leftOrLeft, right)), DistributeAndOverOr(new BinaryExp(BinaryOperator.And, leftOrRight, right)));

            case BinaryExp { Op: BinaryOperator.And, Left: var left, Right: BinaryExp { Op: BinaryOperator.Or, Left: var rightOrLeft, Right: var rightOrRight } }:
                // A AND (B OR C) -> (A AND B) OR (A AND C)
                return new BinaryExp(BinaryOperator.Or, DistributeAndOverOr(new BinaryExp(BinaryOperator.And, left, rightOrLeft)), DistributeAndOverOr(new BinaryExp(BinaryOperator.And, left, rightOrRight)));

            case BinaryExp binaryExp:
                // If neither child is an OR, recurse on the children and return
                return new BinaryExp(binaryExp.Op, DistributeAndOverOr(binaryExp.Left), DistributeAndOverOr(binaryExp.Right));

            default:
                return expr;
        }
    }
}