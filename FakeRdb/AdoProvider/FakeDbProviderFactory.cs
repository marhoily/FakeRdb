namespace FakeRdb;

public sealed class FakeDbProviderFactory : DbProviderFactory
{
    public override DbParameter CreateParameter()
    {
        return new FakeDbParameter();
    }
}