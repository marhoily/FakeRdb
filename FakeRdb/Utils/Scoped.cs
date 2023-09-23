namespace FakeRdb;

public struct Scoped<T> : IDisposable
    where T: notnull
{
    private T? _value;

    public T Value => _value ?? throw new InvalidOperationException("");

    public Scoped<T> Set(T value)
    {
        _value = value;
        return this;
    }

    public void Dispose()
    {
        _value = default;
    }
}