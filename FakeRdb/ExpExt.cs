namespace FakeRdb;

public static class ExpExt
{
    public static object? Resolve(this SQLiteParser.ExprContext ctx, FakeDbParameterCollection parameters)
    {
        if (ctx.BIND_PARAMETER() is { } bind)
        {
            return parameters[bind.GetText()].Value;
        }

        throw new NotImplementedException(ctx.GetText());
    }
}