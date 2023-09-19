namespace FakeRdb;

public abstract class Expression : IResult
{
    protected abstract void SetTarget(Field field);
    protected abstract void SetValue(object value);

    public Expression BindTarget(Field field)
    {
        SetTarget(field);
        return this;
    }
    public Expression BindValue(object value)
    {
        SetValue(value);
        return this;
    }

    public abstract object? Resolve(Row row);
}