namespace FakeRdb;

public sealed class Table : List<Row>
{
    public void Add(object[] oneRow) => Add(new Row(this, oneRow));
}