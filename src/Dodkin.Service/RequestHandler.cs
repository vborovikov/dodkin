namespace Dodkin.Service;

using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Dodkin.Dispatch;
using Relay.RequestModel;
using Relay.RequestModel.Default;

sealed class RequestHandler : DefaultRequestDispatcherBase,
    IAsyncQueryHandler<ServiceStatusQuery, ServiceStatus>
{
    private readonly DbDataSource db;
    private readonly ILogger<RequestHandler> log;

    public RequestHandler(DbDataSource dataSource, ILogger<RequestHandler> logger)
    {
        this.db = dataSource;
        this.log = logger;
    }

    public async Task<ServiceStatus> RunAsync(ServiceStatusQuery query)
    {
        try
        {
            // we check the database connection only since the worker must be running and
            // operating the service queues in order to process this query
            
            await using var cnn = await this.db.OpenConnectionAsync(query.CancellationToken);
            var result = await cnn.ExecuteScalarAsync<int>("select 1;");

            return result == 1 ? ServiceStatus.Operational : ServiceStatus.SemiOperational;
        }
        catch (Exception x)
        {
            this.log.LogError(EventIds.Servicing, x, "Failed to query service status.");
            return ServiceStatus.NonOperational;
        }
    }
}
