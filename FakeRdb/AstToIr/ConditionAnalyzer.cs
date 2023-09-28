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
            if (left is ColumnExp lce1 && right is ColumnExp rce)
                return new EquiJoinCondition(
                    lce1.Table, lce1.FullColumnName,
                    rce.Table, rce.FullColumnName);
            if (left is ColumnExp lce && right is IExpression)
            {
                return new SingleTableCondition(lce.Table, binaryExp);
            }

            if (left is not SingleTableCondition ls ||
                right is not SingleTableCondition rs)
                return binaryExp;
            if (ls.Table == rs.Table)
                return ls with { Filter = new BinaryExp(And, ls.Filter, rs.Filter) };
            if (binaryExp.Op == Equal &&
                ls.Filter is ColumnExp lc &&
                rs.Filter is ColumnExp rc)
                return new EquiJoinCondition(
                    ls.Table, lc.FullColumnName,
                    rs.Table, rc.FullColumnName);
            return binaryExp;
        }
    }
}