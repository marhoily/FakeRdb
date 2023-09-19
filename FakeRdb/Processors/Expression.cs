namespace FakeRdb;

public sealed class Expression : IResult
{
    private object? _value;

    private Expression(object? value)
    {
        _value = value;
    }

    public Expression BindTarget(Field field)
    {
        _value = Convert.ChangeType(_value, field.FieldType);
        return this;
    }

    public object? Resolve(Row row)
    {
        return _value;
    }

    public static IResult Value(object? value)
    {
        return new Expression(value);
    }
}