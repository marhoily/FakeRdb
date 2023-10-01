namespace FakeRdb;

public static class DbCommandExtensions
{
    public static void SetParameter(this DbCommand cmd,
        DbProviderFactory factory, string parameterName, object? value)
    {
        var dbParameter =
            factory.CreateParameter() ??
            throw new InvalidOperationException("WTF?");
        dbParameter.ParameterName = parameterName;
        dbParameter.Value = value;
        cmd.Parameters.Add(dbParameter);
    }
    /* NOTE: Microsoft.Data.Sqlite does not support positional bindings!
     * https://github.com/aspnet/Microsoft.Data.Sqlite/issues/8
     */

}