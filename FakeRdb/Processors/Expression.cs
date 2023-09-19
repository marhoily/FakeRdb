namespace FakeRdb;

public sealed class Expression : IResult
{
    private object? _value = Uninitialized;
    private Field? _field;
    private static readonly object Uninitialized = new();
    private Expression(object? value) => _value = value;
    private Expression(Field value) => _field = value;

    public Expression BindTarget(Field field)
    {
        _field = field;
        return this;
    }
    
    public Expression BindValue(object value)
    {
        _value = value;
        return this;
    }
    
    public object? Resolve(Row row)
    {
        if (_value == Uninitialized)
        {
            if (_field == null) 
                throw new InvalidOperationException(
                    "Cannot resolve value without column");
            _value = row[_field];
        }
        if (_field != null)
            _value = Convert.ChangeType(_value, _field.FieldType);
        return _value;
    }

    public static IResult Value(object? value)
    {
        return new Expression(value);
    }

    public static IResult Column(Field field)
    {
        return new Expression(field);
    }
}