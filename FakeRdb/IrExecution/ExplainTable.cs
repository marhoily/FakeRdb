using static FakeRdb.TypeAffinity;

namespace FakeRdb;

public readonly struct ExplainTable
{
    private readonly Table _table;

    public ExplainTable()
    {
        _table = new("__e__",
            new[]
            {
                new ColumnHeader(0, "id", "e.id", Integer, true),
                new ColumnHeader(1, "parent", "e.parent", Integer),
                new ColumnHeader(2, "notused", "e.notused", Integer),
                new ColumnHeader(3, "detail", "e.detail", Text)
            });
    }

    public ExplainTable(Table table)
    {
        if (table.Name != "__e__") throw new ArgumentException(nameof(table));
        _table = table;
    }

    public ExplainTable Append(string detail)
    {
        _table.Columns[0].Rows.Add(_table.Autoincrement());
        _table.Columns[1].Rows.Add(0);
        _table.Columns[2].Rows.Add(0);
        _table.Columns[3].Rows.Add(detail);
        return this;
    }

    public static implicit operator Table(ExplainTable explain) => explain._table;
}
public static class ExplainTableExtensions
{
    public static ExplainTable AsExplainTable(this Table table) => new(table);
}
