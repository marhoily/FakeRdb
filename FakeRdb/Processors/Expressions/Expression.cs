namespace FakeRdb;

public abstract class Expression : IProjection
{
    protected abstract void SetTarget(Field field);

    public Expression BindTarget(Field field)
    {
        SetTarget(field);
        return this;
    }

    public abstract object? Resolve(params Row[] dataSet);
    public T Resolve<T>(params Row[] dataSet) => (T)Resolve(dataSet)!;
    public abstract SqliteTypeAffinity ExpressionType { get; }
    public abstract string ResultSetName { get; }
}