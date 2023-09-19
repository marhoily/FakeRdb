namespace FakeRdb;

public struct Scope<T> : IDisposable
{
    public T? Value { get; private set; }
    public Scope<T> Set(T? value)
    {
        Value = value;
        return this;
    }

    public void Dispose()
    {
        Value = default;
    }
}