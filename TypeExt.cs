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
public static class TypeExt
{
    public static Type ToRuntimeType(this SQLiteParser.Type_nameContext context)
    {
        return context.GetText() switch
        {
            "TEXT" => typeof(string),
            "INTEGER" => typeof(long),
            var x => throw new ArgumentOutOfRangeException(x)
        };
    }
}