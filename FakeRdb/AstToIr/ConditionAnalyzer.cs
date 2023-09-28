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

            return (left, right) switch
            {
                (ColumnExp l, ColumnExp r) when l.Table != r.Table =>
                    new EquiJoinCondition(
                        l.Table, l.FullColumnName,
                        r.Table, r.FullColumnName),

                (ColumnExp l, IExpression) => new SingleTableCondition(l.Table, binaryExp),

                (SingleTableCondition l, ColumnExp r) when l.Table == r.Table => 
                    l with { Filter = new BinaryExp(binaryExp.Operand, l.Filter, r) },
                (SingleTableCondition l, ColumnExp r) when l.Table != r.Table => 
                    binaryExp,

                _ => throw new InvalidOperationException("Unreachable")
            };
        }

    }
}