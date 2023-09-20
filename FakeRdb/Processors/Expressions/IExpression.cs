namespace FakeRdb;

public interface IExpression : IProjection
{
    public string ResultName { get; }
    public SqliteTypeAffinity ExpressionType { get; }
    public object? Eval();
    public object? Eval(Row dataSet);
    public object? Eval(Row[] dataSet);
    void SetAlias(string value);
}
