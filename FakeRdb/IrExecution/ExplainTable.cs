using static FakeRdb.TypeAffinity;

namespace FakeRdb;

public readonly struct ExplainTable2
{
    private readonly Table _table;

    public ExplainTable2(Table table)
    {
        if (table.Name != "__e__") throw new ArgumentException(nameof(table));
        _table = table;
    }

    public void Insert(string detail)
    {
        _table.Columns[0].Rows.Add(_table.Autoincrement());
        _table.Columns[1].Rows.Add(0);
        _table.Columns[2].Rows.Add(0);
        _table.Columns[3].Rows.Add(detail);
    }
}
public static class ExplainTableExtensions
{
    public static ExplainTable2 AsExplainTable(this Table table) => new(table);
}
public sealed class ExplainTable
{
    private readonly Table _table = new("__e__",
        new[]
        {
            new ColumnHeader(0, "id", "e.id", Integer, true),
            new ColumnHeader(1, "parent", "e.parent", Integer),
            new ColumnHeader(2, "notused", "e.notused", Integer),
            new ColumnHeader(3, "detail", "e.detail", Text)
        });

    private void Insert(string detail)
    {
        _table.Columns[0].Rows.Add(_table.Autoincrement());
        _table.Columns[1].Rows.Add(0);
        _table.Columns[2].Rows.Add(0);
        _table.Columns[3].Rows.Add(detail);
    }

    public ExplainTable With(string row)
    {
        Insert(row);
        return this;
    }

    public Table Build() => _table;
}