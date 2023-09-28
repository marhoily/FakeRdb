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

        var tableSet = tables.ToHashSet();
        var singleTableConditions = new List<SingleTableCondition>();
        var equiJoinConditions = new List<EquiJoinCondition>();
        var generalCondition = default(IExpression);
        foreach (var c in andGroup.Select(DiscriminateCondition))
        {
            switch (c)
            {
                case SingleTableCondition stc:
                    singleTableConditions.Add(stc);
                    if (!tableSet.Remove(stc.Table))
                        throw new InvalidOperationException();
                    break;
                case EquiJoinCondition ejc:
                    equiJoinConditions.Add(ejc);
                    break;
                case IExpression exp:
                    generalCondition = Expr.And(generalCondition, exp);
                    break;
            }   
        }

        singleTableConditions.AddRange(tableSet.Select(
            table => new SingleTableCondition(table, Expr.True)));
        var compositeCondition =
            new CompositeCondition(
                singleTableConditions.ToArray(),
                equiJoinConditions.ToArray(),
                filter);
        return new[] { compositeCondition };
    }

    private static ITaggedCondition DiscriminateCondition(IExpression exp)
    {
        switch (exp)
        {
            case BindExp:
            case AggregateExp:
                return exp;
            case BinaryExp binaryExp:
                var left = DiscriminateCondition(binaryExp.Left);
                var right = DiscriminateCondition(binaryExp.Right);
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
            case ColumnExp col:
                return new SingleTableCondition(col.Table, col);
            case InExp:
            case LiteralExp:
            case ScalarExp:
                return exp;
            case UnaryExp unaryExp:
                return DiscriminateCondition(unaryExp.Operand);
            default:
                throw new ArgumentOutOfRangeException(nameof(exp));
        }
    }
}