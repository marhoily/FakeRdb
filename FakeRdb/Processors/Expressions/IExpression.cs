namespace FakeRdb;

public interface IExpression : IProjection
{
    public string ResultSetName { get; }
    public SqliteTypeAffinity ExpressionType { get; }
    public object? Eval(params Row[] dataSet);
}
