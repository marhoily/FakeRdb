namespace FakeRdb;

public sealed class Table : List<Row>
{
    public Table(Field[] schema)
    {
        Schema = schema;
    }

    public Field[] Schema { get; set; }
    public void Add(object[] oneRow) => Add(new Row(this, oneRow));
}