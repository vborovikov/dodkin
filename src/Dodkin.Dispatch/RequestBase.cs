namespace Dodkin.Dispatch;

using System.Text.Json.Serialization;
using System.Threading;
using Relay.RequestModel;

/// <summary>
/// Represents a request.
/// </summary>
public abstract record RequestBase : IRequest
{
    /// <summary>
    /// Gets or sets the cancellation token associated with the processing of the request.
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; }
}

/// <summary>
/// Represents a command.
/// </summary>
public abstract record Command : RequestBase, ICommand { }

/// <summary>
/// Represents a query.
/// </summary>
/// <typeparam name="T">The type of the query result.</typeparam>
public abstract record Query<T> : RequestBase, IQuery<T> { }

/// <summary>
/// Represents a service status.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// The service is unreachable.
    /// </summary>
    Unreachable,
    /// <summary>
    /// The service is operational.
    /// </summary>
    Operational,
    /// <summary>
    /// The service is semi-operational, i.e. it has some non-critical components that are not operational.
    /// </summary>
    SemiOperational,
    /// <summary>
    /// The service is non-operational, i.e. it has critical components that are not operational.
    /// </summary>
    NonOperational,
    /// <summary>
    /// The service is busy working.
    /// </summary>
    Busy,
}

/// <summary>
/// Represents a service status query.
/// </summary>
public record ServiceStatusQuery : Query<ServiceStatus> { }

/// <summary>
/// Extension methods for <see cref="IRequestDispatcher"/>.
/// </summary>
public static class RequestDispatcherExtensions
{
    /// <summary>
    /// Gets the service status.
    /// </summary>
    /// <param name="requestDispatcher">The request dispatcher.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    public static async Task<ServiceStatus> GetStatusAsync(this IRequestDispatcher requestDispatcher)
    {
        try
        {
            return await requestDispatcher.RunAsync(new ServiceStatusQuery());
        }
        catch (TimeoutException)
        {
            return ServiceStatus.Unreachable;
        }
    }
}