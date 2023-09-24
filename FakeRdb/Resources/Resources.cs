namespace FakeRdb;

public static class Resources
{
    public static InvalidOperationException ColumnNotFound(string name)
    {
        return new InvalidOperationException($"Column {name} is not found");
    }
    public static InvalidOperationException AmbiguousColumnReference(string name)
    {
        return new InvalidOperationException($"Ambiguous column ref: {name}");
    }

}