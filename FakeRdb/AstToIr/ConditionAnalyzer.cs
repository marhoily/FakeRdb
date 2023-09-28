using static FakeRdb.BinaryOperator;
using static FakeRdb.IR;

namespace FakeRdb;

public static class ConditionAnalyzer
{
    public static CompositeCondition[] BuildAlternativeSources(
        IEnumerable<Table> tables, IExpression? filter)
    {
        var orGroup = filter?.ToDnf().DecomposeDnf();
        var andGroup = orGroup?.Alternatives
            .Single().Conditions ?? Array.Empty<IExpression>();

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
        return new[] { compositeCondition };
    }

    public static ITaggedCondition DiscriminateCondition(IExpression exp)
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
                (ColumnExp lce1, ColumnExp rce) =>
                    new EquiJoinCondition(
                        lce1.Table, lce1.FullColumnName,
                        rce.Table, rce.FullColumnName),

                (ColumnExp lce, IExpression) => new SingleTableCondition(lce.Table, binaryExp),

                (not SingleTableCondition, _) => binaryExp,

                _ => throw new InvalidOperationException("Unreachable")
            };
        }

    }
}