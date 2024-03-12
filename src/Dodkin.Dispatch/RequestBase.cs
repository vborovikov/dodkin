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
