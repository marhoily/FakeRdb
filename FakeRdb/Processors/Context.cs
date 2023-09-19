namespace FakeRdb;

public struct Context<T> : IDisposable
{
    public T? Value { get; private set; }
    public Context<T> Set(T? value)
    {
        Value = value;
        return this;
    }

    public void Dispose()
    {
        Value = default;
    }
}