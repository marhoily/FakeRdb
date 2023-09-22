namespace FakeRdb;

public interface IExpression : IProjection
{
    public object? Eval();
    public object? Eval(Row dataSet);
    public object? Eval(Row[] dataSet);
}
