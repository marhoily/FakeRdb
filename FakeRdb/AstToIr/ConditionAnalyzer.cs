using static FakeRdb.BinaryOperator;
using static FakeRdb.IR;

namespace FakeRdb;

public static class ConditionAnalyzer
{
    public static CompositeCondition[] BuildAlternativeSources(
        IEnumerable<Table> tables, IExpression? filter)
    {
        var orGroup = filter?.ToDnf().DecomposeDnf();
        if (orGroup == null)
            return new[] { BuildCompositeCondition(tables, Array.Empty<IExpression>()) };

        var andGroup = orGroup.Alternatives
            .Select(alt => BuildCompositeCondition(tables, alt.Conditions))
            .ToArray();

        return andGroup;
    }

    private static CompositeCondition BuildCompositeCondition(IEnumerable<Table> tables, IExpression[] andGroup)
    {
        var singleTableConditions = new Dictionary<Table, SingleTableCondition>();
        var equiJoinConditions = new List<EquiJoinCondition>();
        var generalCondition = default(IExpression);
        foreach (var c in andGroup.Select(DiscriminateCondition))
        {
            switch (c)
            {
                case SingleTableCondition stc:
                    singleTableConditions.Add(stc.Table,
                        singleTableConditions.Remove(stc.Table, out var old)
                            ? stc with { Filter = new BinaryExp(And, stc.Filter, old.Filter) }
                            : stc);
                    break;
                case EquiJoinCondition ejc:
                    equiJoinConditions.Add(ejc);
                    break;
                case GeneralCondition gc:
                    generalCondition = gc.Filter;
                    break;
                case IExpression exp:
                    generalCondition = Expr.And(generalCondition, exp);
                    break;
            }
        }

        var extra = tables
            .Except(singleTableConditions.Keys)
            .Select(table => new SingleTableCondition(table, Expr.True));
        foreach (var stc in extra)
            singleTableConditions.Add(stc.Table, stc);

        var compositeCondition =
            new CompositeCondition(
                singleTableConditions.Values.ToArray(),
                equiJoinConditions.ToArray(),
                generalCondition);
        return compositeCondition;
    }

    private static ITaggedCondition DiscriminateCondition(IExpression exp)
    {
        return exp switch
        {
            BinaryExp binaryExp => Binary(binaryExp),
            UnaryExp unaryExp => DiscriminateCondition(unaryExp.Operand),
            _ => exp,
        };

        static ITaggedCondition Binary(BinaryExp binaryExp)
        {
            var left = DiscriminateCondition(binaryExp.Left);
            var right = DiscriminateCondition(binaryExp.Right);
            
            // Thanks to this the complexity of the upcoming search can
            // be cut in half. We cannot forget if the swap occurred,
            // because many binary operators are not commutative.
            var reflect = Priority(left) < Priority(right);
            var (hi, lo) = reflect ? (right, left) : (left, right);

            // to exhaust all combinations go in order of decreasing 
            // priorities of the both arguments 
            return (hi, lo) switch
            {
                (GeneralCondition or EquiJoinCondition, _) =>
                    new GeneralCondition(binaryExp),

                (SingleTableCondition l, ColumnExp r)
                    when l.Table == r.Table => l with
                    {
                        Filter = reflect
                            ? new BinaryExp(binaryExp.Operand, l.Filter, r)
                            : new BinaryExp(binaryExp.Operand, r, l.Filter)
                    },

                (SingleTableCondition l, ColumnExp r)
                    when l.Table != r.Table => new GeneralCondition(binaryExp),

                (SingleTableCondition l, IExpression r) =>
                    l with
                    {
                        Filter = reflect
                            ? new BinaryExp(binaryExp.Operand, l.Filter, r)
                            : new BinaryExp(binaryExp.Operand, r, l.Filter)
                    },

                (ColumnExp l, ColumnExp r) when l.Table != r.Table =>
                    new EquiJoinCondition(
                        l.Table, l.FullColumnName,
                        r.Table, r.FullColumnName),
                (ColumnExp l, ColumnExp r) when l.Table == r.Table =>
                    new SingleTableCondition(l.Table, binaryExp),

                (ColumnExp l, IExpression) =>
                    new SingleTableCondition(l.Table, binaryExp),

                // ConditionAnalyzer pre-calculates constant expressions
                (IExpression, IExpression) =>
                    new BindExp(binaryExp.Eval(TypeAffinity.Integer)),

                _ => throw new InvalidOperationException(
                    $"Unreachable: " +
                    $"HI = {hi.GetType().Name}; " +
                    $"LO = {lo.GetType().Name}")
            };
        }

        static int Priority(ITaggedCondition exp)
        {
            return exp switch
            {
                GeneralCondition => 40,
                EquiJoinCondition => 30,
                SingleTableCondition => 20,
                ColumnExp => 10,
                _ => 0
            };
        }
    }
}