namespace Dodkin.Dispatch;

using System.Text.Json.Serialization;
using System.Threading;
using Relay.RequestModel;

public abstract record RequestBase : IRequest
{
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; }
}

public abstract record Command : RequestBase, ICommand { }

public abstract record Query<T> : RequestBase, IQuery<T> { }
