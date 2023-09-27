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
}