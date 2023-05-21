namespace Dodkin.Service.Data;
using System.Data.Common;
using System.Threading.Tasks;

interface IDbFactory
{
    DbProviderFactory ProviderFactory { get; }
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}

sealed class DbFactory : IDbFactory
{
    private readonly string connectionString;

    public DbFactory(DbProviderFactory dbProviderFactory, string connectionString)
    {
        this.ProviderFactory = dbProviderFactory;
        this.connectionString = connectionString;
    }

    public DbProviderFactory ProviderFactory { get; }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var cnn = this.ProviderFactory.CreateConnection()!;
        cnn.ConnectionString = this.connectionString;
        await cnn.OpenAsync(cancellationToken);
        return cnn;
    }
}
