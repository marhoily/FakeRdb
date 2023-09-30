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
    public static void AddPositionalParameters(this DbCommand cmd,
        DbProviderFactory factory, object?[] positionalBindings)
    {
        foreach (var binding in positionalBindings) 
            cmd.AddPositionalParameter(factory, binding);
    }
    public static void AddPositionalParameter(this DbCommand cmd,
        DbProviderFactory factory, object? value)
    {
        var dbParameter =
            factory.CreateParameter() ??
            throw new InvalidOperationException("WTF?");
        dbParameter.Value = value;
        cmd.Parameters.Add(dbParameter);
    }
}