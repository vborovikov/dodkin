namespace Dodkin.Dispatch;

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;
using Relay.RequestModel.Default;

public class UdsRequestHandler : UdsOperator, IRequestDispatcher
{
    private readonly ILogger log;
    private readonly Socket socket;
    private readonly IRequestDispatcher dispatcher;

    public UdsRequestHandler(string socketPath, ILogger logger)
        : this(socketPath, requestDispatcher: default, logger)
    {
    }

    public UdsRequestHandler(string socketPath, IRequestDispatcher? requestDispatcher, ILogger logger)
    {
        this.Endpoint = new(socketPath);
        this.dispatcher = requestDispatcher ?? new InternalRequestDispatcher(this);
        this.log = logger;

        if (File.Exists(socketPath))
        {
            File.Delete(socketPath);
        }

        this.socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
        this.socket.Bind(this.Endpoint);
        this.socket.Listen(MaxConnections);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        this.socket.Dispose();
    }

    public UnixDomainSocketEndPoint Endpoint { get; }

    /// <summary>
    /// Starts processing request messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.log.LogInformation(EventIds.DispatchingStarted, "Dispatching requests started");

            await Parallel.ForEachAsync(AcceptAllAsync(this.socket, default), cancellationToken, async (client, cancellationToken) =>
            {
                using var buffer = new Utf8MemoryBuffer();
                try
                {
                    var bytesReceived = await client.ReceiveAsync(buffer.GetMemory(), cancellationToken).ConfigureAwait(false);
                    if (bytesReceived <= 0)
                    {
                        this.log.LogWarning(EventIds.MessageFailed, "Encountered empty message");
                        return;
                    }
                    buffer.Advance(bytesReceived);

                    if (TryRead<IRequest>(buffer.WrittenSpan, out var request) && request is RequestBase mutableRequest)
                    {
                        mutableRequest.CancellationToken = cancellationToken;
                    }

                    buffer.Clear();
                    if (request is ICommand command)
                    {
                        try
                        {
                            await ExecuteCommandAsync(command);
                            //todo: RunInBackground(Task.Run(() => ExecuteCommandAsync(command), cancellationToken: default));
                            buffer.TryAppend("ack");
                        }
                        catch (Exception x)
                        {
                            this.log.LogError(EventIds.CommandExecutionFailed, x, "Error executing command");

                            buffer.TryAppend(x.GetType().AssemblyQualifiedName);
                            buffer.TryAppend(PayloadSeparator);
                            buffer.TrySerialize(x);
                        }
                    }
                    else if (request is IQuery query)
                    {
                        try
                        {
                            var result = await RunQueryAsync(query).ConfigureAwait(false);

                            if (result is not null)
                            {
                                buffer.TryAppend(result.GetType().AssemblyQualifiedName);
                                buffer.TryAppend(PayloadSeparator);
                                buffer.TrySerialize(result);
                            }
                            else
                            {
                                buffer.TryAppend(typeof(object).AssemblyQualifiedName);
                                buffer.TryAppend(PayloadSeparator);
                                buffer.TryAppend("{}");
                            }
                        }
                        catch (Exception x)
                        {
                            this.log.LogError(EventIds.QueryExecutionFailed, x, "Error executing query");

                            buffer.TryAppend(x.GetType().AssemblyQualifiedName);
                            buffer.TryAppend(PayloadSeparator);
                            buffer.TrySerialize(x);
                        }
                    }
                    else
                    {
                        buffer.TryAppend("what");
                        this.log.LogWarning(EventIds.MessageRejected, "Rejected unrecognized message");
                    }

                    if (buffer.WrittenCount > 0)
                    {
                        await client.SendAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception x) when (x is not OperationCanceledException)
                {
                    this.log.LogError(EventIds.MessageFailed, x, "Error handling request");
                }
                finally
                {
                    client.Dispose();
                }
            });
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error dispatching requests");
            throw;
        }
        finally
        {
            this.log.LogInformation(EventIds.DispatchingStopped, "Dispatching requests stopped");
        }

        static async IAsyncEnumerable<Socket> AcceptAllAsync(Socket server, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                Socket? socket = null;
                try
                {
                    socket = await server.AcceptAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    yield break;
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
                {
                    yield break;
                }
                catch (SocketException)
                {
                }

                if (socket is not null)
                {
                    yield return socket;
                }
            }
        }
    }

    /// <summary>System.Net.Sockets.SocketException: 'The I/O operation has been aborted because of either a thread exit or an application request.'
    /// Runs the specified query and returns the result.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The result of the query.</returns>
    protected Task<object> RunQueryAsync(IQuery query) =>
        this.dispatcher.RunGenericAsync(query);

    /// <summary>
    /// Executes the specified command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected Task ExecuteCommandAsync(ICommand command) =>
        this.dispatcher.ExecuteGenericAsync(command);

    Task<TResult> IRequestDispatcher.RunAsync<TResult>(IQuery<TResult> query) =>
        this.dispatcher.RunAsync(query);

    Task IRequestDispatcher.ExecuteAsync<TCommand>(TCommand command) =>
        this.dispatcher.ExecuteAsync(command);

    private static void RunInBackground(Task task)
    {
        if (!task.IsCompleted || task.IsFaulted)
        {
            _ = RunAwaited(task);
        }

        async static Task RunAwaited(Task task)
        {
            await task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private sealed class InternalRequestDispatcher : DefaultRequestDispatcherBase
    {
        private readonly UdsRequestHandler requestHandler;

        public InternalRequestDispatcher(UdsRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        protected override object GetRequestHandler(Type requestHandlerType) => this.requestHandler;
    }
}
