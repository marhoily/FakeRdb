namespace FakeRdb;

public static class Exceptions
{
    public static InvalidOperationException ColumnNotFound(string name)
    {
        return new InvalidOperationException($"Column {name} is not found");
    }

}