namespace FakeRdb;

public abstract class Expression : IProjection
{
    public abstract string ResultSetName { get; }
    public abstract SqliteTypeAffinity ExpressionType { get; }
    public abstract object? Resolve(params Row[] dataSet);
    public T Resolve<T>(params Row[] dataSet) => (T)Resolve(dataSet)!;
}