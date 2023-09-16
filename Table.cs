namespace FakeRdb;

public sealed class Table : List<Row>
{
    public Table(Field[] schema)
    {
        Schema = schema;
    }

    public Field[] Schema { get; set; }

    public Field GetColumn(string name) => Array.Find(Schema, f =>
                                               string.Equals(f.Name, name,
                                                   StringComparison.InvariantCultureIgnoreCase)) ??
                                           throw new InvalidOperationException($"Column {name} is not found");
    public void Add(object?[] oneRow) => Add(new Row(this, oneRow));

}