namespace Dodkin.Dispatch;

using System.Text.Json.Serialization;
using System.Threading;
using Relay.RequestModel;

public abstract class RequestBase : IRequest
{
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; }
}

public abstract class Command : RequestBase, ICommand { }

public abstract class Query<T> : RequestBase, IQuery<T> { }
