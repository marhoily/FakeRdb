using static FakeRdb.IR;

namespace FakeRdb;

public static class ConditionAnalyzer
{
    public static CompositeCondition[] BuildAlternativeSources(
        IEnumerable<Table> tables, IExpression? filter)
    {
        var singleTableConditions = tables
            .Select(table => new SingleTableCondition(table, Expr.True))
            .ToArray();
        var compositeCondition = 
            new CompositeCondition(
                singleTableConditions,
                Array.Empty<EquiJoinCondition>(),
                filter);
        return new []{compositeCondition};
    }

    public static bool AreEquivalent(IExpression x, IExpression y)
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
    
    public static IExpression SimplifyIdempotent(IExpression expr)
    {
        // Base case: If the expression is a leaf node, return as is
        if (expr is not BinaryExp binaryExp)
            return expr;

        // Recursive case: First, simplify the children
        var simplifiedLeft = SimplifyIdempotent(binaryExp.Left);
        var simplifiedRight = SimplifyIdempotent(binaryExp.Right);

        // Then, check for idempotence
        if ((binaryExp.Op == BinaryOperator.And || binaryExp.Op == BinaryOperator.Or) &&
            AreEquivalent(simplifiedLeft, simplifiedRight))
        {
            return simplifiedLeft;
        }

        // If no idempotent simplification was possible, return a simplified version of the original expression
        return new BinaryExp(binaryExp.Op, simplifiedLeft, simplifiedRight);
    }

    public static IExpression ApplyDeMorgansLaw(IExpression exp)
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
    
    public static IExpression ToCnf(this IExpression expr)
    {
        var afterDeMorgans = ApplyDeMorgansLaw(expr);
        var cnf = DistributeOrOverAnd(afterDeMorgans);
        return SimplifyIdempotent(cnf);
    }

    public static IExpression DistributeOrOverAnd(IExpression expr)
    {
        switch (expr)
        {
            case BinaryExp { Op: BinaryOperator.Or, Left: BinaryExp { Op: BinaryOperator.And, Left: var leftAndLeft, Right: var leftAndRight }, Right: var right }:
                // (A AND B) OR C -> (A OR C) AND (B OR C)
                return new BinaryExp(BinaryOperator.And, 
                    DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, leftAndLeft, right)), 
                    DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, leftAndRight, right)));

            case BinaryExp { Op: BinaryOperator.Or, Left: var left, Right: BinaryExp { Op: BinaryOperator.And, Left: var rightAndLeft, Right: var rightAndRight } }:
                // A OR (B AND C) -> (A OR B) AND (A OR C)
                return new BinaryExp(BinaryOperator.And, 
                    DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, left, rightAndLeft)), 
                    DistributeOrOverAnd(new BinaryExp(BinaryOperator.Or, left, rightAndRight)));
        
            case BinaryExp binaryExp:
                // If neither child is an AND, recurse on the children and return
                return new BinaryExp(binaryExp.Op, 
                    DistributeOrOverAnd(binaryExp.Left), 
                    DistributeOrOverAnd(binaryExp.Right));

            default:
                return expr;
        }
    }


}