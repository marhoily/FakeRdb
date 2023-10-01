using static FakeRdb.TypeAffinity;

namespace FakeRdb;

public sealed class ExplainTable
{
    private readonly Table _table = new("e",
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