using System.Data.Common;

namespace FakeRdb;

public sealed class FakeDbProviderFactory : DbProviderFactory
{
    public override DbParameter CreateParameter()
    {
        return new FakeDbParameter();
    }
}