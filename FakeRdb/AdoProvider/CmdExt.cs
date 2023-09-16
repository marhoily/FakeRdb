namespace FakeRdb;

public static class CmdExt
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

    public static (DbDataReader?, Exception?) SafeExecuteReader(this DbCommand cmd)
    {
        try
        {
            return (cmd.ExecuteReader(), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }
}