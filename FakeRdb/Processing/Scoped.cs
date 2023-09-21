namespace FakeRdb;

public struct Scoped<T> : IDisposable
{
    public T? Value { get; private set; }
    public Scoped<T> Set(T? value)
    {
        Value = value;
        return this;
    }

    public void Dispose()
    {
        Value = default;
    }
}