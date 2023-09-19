namespace FakeRdb;

public static class ExpExt
{
    public static object? Resolve(this SQLiteParser.ExprContext ctx, 
        FakeDbParameterCollection parameters,
        Type fieldType)
    {
        if (ctx.BIND_PARAMETER() is { } bind)
        {
            return parameters[bind.GetText()].Value;
        }
        if (ctx.literal_value() is {} literal)
        {
            return Convert.ChangeType(literal.GetText().Unquote(), fieldType);
        }

        throw new NotImplementedException(ctx.GetText());
    }
    public static object? Resolve(this SQLiteParser.ExprContext ctx, 
        FakeDbParameterCollection parameters)
    {
        if (ctx.BIND_PARAMETER() is { } bind)
        {
            return parameters[bind.GetText()].Value;
        }
        if (ctx.literal_value() is {} literal)
        {
            return literal.GetText().Unquote();
        }

        throw new NotImplementedException(ctx.GetText());
    }
}