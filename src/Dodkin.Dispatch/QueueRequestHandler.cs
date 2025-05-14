namespace Dodkin.Dispatch;

using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;
using Relay.RequestModel.Default;

/// <summary>
/// Represents a queue operator that dispatches requests to the appropriate handlers.
/// </summary>
public class QueueRequestHandler : QueueOperator, IRequestDispatcher
{
    private const int ResponseCacheCapacity = 8;

    /// <summary>
    /// The name of the subqueue used for invalid messages.
    /// </summary>
    private const string InvalidSubqueueName = "invalid";
    /// <summary>
    /// The name of the subqueue used for commands.
    /// </summary>
    private const string CommandSubqueueName = "commands";
    /// <summary>
    /// The name of the subqueue used for queries.
    /// </summary>
    private const string QuerySubqueueName = "queries";
    /// <summary>
    /// The name of the subqueue used for deferring requests specific to the derived type.
    /// </summary>
    protected const string RequestSubqueueName = "requests";

    private sealed class InternalRequestDispatcher : DefaultRequestDispatcherBase
    {
        private readonly QueueRequestHandler requestHandler;

        public InternalRequestDispatcher(QueueRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        protected override object GetRequestHandler(Type requestHandlerType) => this.requestHandler;
    }

    private readonly IMessageQueueFactory mq;
    private readonly ILogger log;
    private readonly IRequestDispatcher dispatcher;
    private readonly LRUCache<MessageQueueName, IMessageQueueWriter> responseCache;

    /// <summary>
    /// Creates new instance of <see cref="QueueRequestHandler"/>.
    /// </summary>
    /// <param name="requestDispatcher">The dispatcher used to dispatch requests to the appropriate handlers.</param>
    /// <param name="endpoint">The endpoint describing the queues used by the handler.</param>
    /// <param name="logger">The logger.</param>
    public QueueRequestHandler(IRequestDispatcher requestDispatcher, MessageEndpoint endpoint, ILogger logger)
        : this(endpoint, MessageQueueFactory.Instance, logger, requestDispatcher) { }

    /// <summary>
    /// Creates new instance of <see cref="QueueRequestHandler"/>.
    /// </summary>
    /// <param name="requestDispatcher">The dispatcher used to dispatch requests to the appropriate handlers.</param>
    /// <param name="endpoint">The endpoint describing the queues used by the handler.</param>
    /// <param name="messageQueueFactory">The message queue factory.</param>
    /// <param name="logger">The logger.</param>
    public QueueRequestHandler(IRequestDispatcher requestDispatcher, MessageEndpoint endpoint, IMessageQueueFactory messageQueueFactory, ILogger logger)
        : this(endpoint, messageQueueFactory, logger, requestDispatcher) { }

    /// <summary>
    /// Creates new instance of <see cref="QueueRequestHandler"/>
    /// </summary>
    /// <param name="endpoint">The endpoint describing the queues used by the handler.</param>
    /// <param name="logger">The logger.</param>
    protected QueueRequestHandler(MessageEndpoint endpoint, ILogger logger)
        : this(endpoint, MessageQueueFactory.Instance, logger, null)
    {
    }

    /// <summary>
    /// Creates new instance of <see cref="QueueRequestHandler"/>
    /// </summary>
    /// <param name="endpoint">The endpoint describing the queues used by the handler.</param>
    /// <param name="messageQueueFactory">The message queue factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="requestDispatcher">The dispatcher used to dispatch requests to the appropriate handlers.</param>
    protected QueueRequestHandler(MessageEndpoint endpoint, IMessageQueueFactory messageQueueFactory, ILogger logger, IRequestDispatcher? requestDispatcher = null)
        : base(endpoint, logger)
    {
        this.mq = messageQueueFactory;
        this.log = logger;
        this.dispatcher = requestDispatcher ?? new InternalRequestDispatcher(this);
        this.responseCache = new(ResponseCacheCapacity);
    }

    /// <summary>
    /// The message properties to read on the application queue.
    /// </summary>
    protected MessageProperty ReadProperties { get; init; } = MessageProperty.TimeToBeReceived;

    /// <summary>
    /// The message properties to peek on the application queue.
    /// </summary>
    protected MessageProperty PeekProperties { get; init; } = MessageProperty.None;

    /// <summary>
    /// The <see cref="ParallelOptions"/> used when processing requests.
    /// </summary>
    public ParallelOptions ParallelOptions { get; init; } = new();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        this.responseCache.Dispose();
    }

    /// <summary>
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

    /// <summary>
    /// Starts processing request messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.log.LogInformation(EventIds.ProcessingStarted, "Processing started");

            await Task.WhenAll(GetProcessingTasks(cancellationToken)).ConfigureAwait(false);
        }
        finally
        {
            this.log.LogInformation(EventIds.ProcessingStopped, "Processing stopped");
        }
    }

    /// <summary>
    /// Gets the request processing tasks operating on the application queue.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The collection of processing tasks.</returns>
    protected virtual ICollection<Task> GetProcessingTasks(CancellationToken cancellationToken)
    {
        return new List<Task>
        {
            DispatchRequestsAsync(cancellationToken),
            ProcessCommandsAsync(cancellationToken),
            ProcessQueriesAsync(cancellationToken),
            ProcessInvalidAsync(cancellationToken),
        };
    }

    private async Task DispatchRequestsAsync(CancellationToken cancellationToken)
    {
        using var appQ = this.mq.CreateSorter(this.Endpoint.ApplicationQueue);
        using var appCQ = this.mq.CreateCursor(appQ);
        using var cmdSQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(CommandSubqueueName));
        using var qrySQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(QuerySubqueueName));
        using var reqSQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(RequestSubqueueName));
        using var invSQ = new Subqueue(this.Endpoint.InvalidMessageQueue ?? this.Endpoint.ApplicationQueue.GetSubqueueName(InvalidSubqueueName));

        try
        {
            this.log.LogInformation(EventIds.DispatchingStarted, "Dispatching requests started");

            await Parallel.ForEachAsync(appCQ.PeekAllAsync(MessageProperty.LookupId | MessageProperty.Extension | this.PeekProperties), CreateParallelOptions<IRequest>(cancellationToken),
                (msg, cancellationToken) =>
                {
                    this.log.LogInformation(EventIds.MessageReceived, "Received message [{MessageLookupId}]", msg.LookupId);

                    using var tx = new QueueTransaction();
                    try
                    {
                        if (CanDispatchRequest(msg))
                        {
                            try
                            {
                                if (TryDispatchRequest(msg))
                                {
                                    this.log.LogInformation(EventIds.RequestDispatched, "Dispatched request [{MessageLookupId}]", msg.LookupId);
                                }
                                else
                                {
                                    appQ.Move(msg.LookupId, reqSQ, tx);
                                    this.log.LogInformation(EventIds.RequestDeferred, "Deferred request [{MessageLookupId}]", msg.LookupId);
                                }
                            }
                            catch (Exception x)
                            {
                                this.log.LogError(EventIds.RequestDispatchFailed, x, "Error dispatching request [{MessageLookupId}]", msg.LookupId);
                                appQ.Move(msg.LookupId, invSQ, tx);
                            }
                        }
                        else
                        {
                            var requestType = FindBodyType(msg);
                            if (typeof(ICommand).IsAssignableFrom(requestType))
                            {
                                appQ.Move(msg.LookupId, cmdSQ, tx);
                                this.log.LogInformation(EventIds.CommandDispatched, "Dispatched command [{MessageLookupId}]", msg.LookupId);
                            }
                            else if (typeof(IQuery).IsAssignableFrom(requestType))
                            {
                                appQ.Move(msg.LookupId, qrySQ, tx);
                                this.log.LogInformation(EventIds.QueryDispatched, "Dispatched query [{MessageLookupId}]", msg.LookupId);
                            }
                            else
                            {
                                appQ.Reject(msg.LookupId, tx);
                                this.log.LogWarning(EventIds.MessageRejected, "Rejected message [{MessageLookupId}]", msg.LookupId);
                            }
                        }

                        tx.Commit();
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        tx.Abort();
                        this.log.LogError(EventIds.MessageFailed, x, "Error dispatching message [{MessageLookupId}]", msg.LookupId);
                    }

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error dispatching messages");
            throw;
        }
        finally
        {
            this.log.LogInformation(EventIds.DispatchingStopped, "Dispatching requests stopped");
        }
    }

    /// <summary>
    /// Determines whether the message should be dispatched as a request.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns><see langword="true"/> if the message should be dispatched as a request; otherwise, <see langword="false"/>.</returns>
    protected virtual bool CanDispatchRequest(in Message message) => false;

    /// <summary>
    /// Tries to dispatch the message as a request.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns><see langword="true"/> if the message was dispatched as a request; otherwise, <see langword="false"/>.</returns>
    protected virtual bool TryDispatchRequest(in Message message) => false;

    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.log.LogInformation(EventIds.DispatchingStarted, "Processing commands started");

            using var commandQ = this.mq.CreateReader(this.Endpoint.ApplicationQueue.GetSubqueueName(CommandSubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await Parallel.ForEachAsync(commandQ.ReadAllAsync(MessageProperties | this.ReadProperties), CreateParallelOptions<ICommand>(cancellationToken),
                async (message, cancellationToken) =>
                {
                    try
                    {
                        using var request = ReadRequest(message, cancellationToken);
                        await ExecuteCommandAsync(request.GetCommand()).ConfigureAwait(false);
                        this.log.LogInformation(EventIds.CommandExecuted, "Executed command <{MessageId}>[{MessageLookupId}]",
                            message.Id, message.LookupId);
                    }
                    catch (OperationCanceledException x) when (!cancellationToken.IsCancellationRequested)
                    {
                        this.log.LogWarning(EventIds.CommandExecutionCancelled, x, "Command <{MessageId}>[{MessageLookupId}] cancelled",
                            message.Id, message.LookupId);
                        deadLetterQ.Write(WrapPoisonMessage(message, x), QueueTransaction.SingleMessage);
                    }
                    catch (Exception x) when (x is NotImplementedException || x.GetBaseException() is NotImplementedException)
                    {
                        this.log.LogWarning(EventIds.CommandNotImplemented, x, "Command <{MessageId}>[{MessageLookupId}] handler not implemented",
                            message.Id, message.LookupId);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogError(EventIds.CommandExecutionFailed, x, "Error executing command <{MessageId}>[{MessageLookupId}]",
                            message.Id, message.LookupId);
                        deadLetterQ.Write(WrapPoisonMessage(message, x), QueueTransaction.SingleMessage);
                    }
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error processing commands");
            throw;
        }
        finally
        {
            this.log.LogInformation(EventIds.DispatchingStopped, "Processing commands stopped");
        }
    }

    private async Task ProcessQueriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.log.LogInformation(EventIds.DispatchingStarted, "Processing queries started");

            using var queryQ = this.mq.CreateReader(this.Endpoint.ApplicationQueue.GetSubqueueName(QuerySubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await Parallel.ForEachAsync(queryQ.ReadAllAsync(MessageProperties | this.ReadProperties), CreateParallelOptions<IQuery>(cancellationToken),
                async (message, cancellationToken) =>
                {
                    try
                    {
                        using var request = ReadRequest(message, cancellationToken);
                        var result = await RunQueryAsync(request.GetQuery()).ConfigureAwait(false);
                        this.log.LogInformation(EventIds.QueryExecuted, "Executed query <{MessageId}>[{MessageLookupId}]",
                            message.Id, message.LookupId);

                        using var response = CreateMessage(result, message.Id);
                        var responseQ = GetResponseQueue(MessageQueueName.Parse(message.ResponseQueue));
                        responseQ.Write(response, QueueTransaction.SingleMessage);
                        this.log.LogInformation(EventIds.QueryResultSent, "Sent query <{MessageId}>[{MessageLookupId}] result",
                            message.Id, message.LookupId);
                    }
                    catch (OperationCanceledException x) when (!cancellationToken.IsCancellationRequested)
                    {
                        this.log.LogWarning(EventIds.QueryExecutionCancelled, x, "Query <{MessageId}>[{MessageLookupId}] cancelled",
                            message.Id, message.LookupId);
                        deadLetterQ.Write(WrapPoisonMessage(message, x), QueueTransaction.SingleMessage);
                    }
                    catch (Exception x) when (x is NotImplementedException || x.GetBaseException() is NotImplementedException)
                    {
                        this.log.LogWarning(EventIds.QueryNotImplemented, x, "Query <{MessageId}>[{MessageLookupId}] handler not implemented",
                            message.Id, message.LookupId);
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogError(EventIds.QueryExecutionFailed, x, "Error executing query <{MessageId}>[{MessageLookupId}]",
                            message.Id, message.LookupId);
                        deadLetterQ.Write(WrapPoisonMessage(message, x), QueueTransaction.SingleMessage);
                    }
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error processing queries");
            throw;
        }
        finally
        {
            this.log.LogInformation(EventIds.DispatchingStopped, "Processing queries stopped");
        }
    }

    private async Task ProcessInvalidAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.log.LogInformation(EventIds.DispatchingStarted, "Processing invalid requests started");

            using var invalidQ = this.mq.CreateReader(this.Endpoint.InvalidMessageQueue ?? this.Endpoint.ApplicationQueue.GetSubqueueName(InvalidSubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await Parallel.ForEachAsync(invalidQ.ReadAllAsync(MessageProperties | this.ReadProperties), CreateParallelOptions<IRequest>(cancellationToken),
                (message, _) =>
                {
                    this.log.LogWarning(EventIds.MessageRejected, "Rejected invalid request <{MessageId}>[{MessageLookupId}]",
                        message.Id, message.LookupId);

                    deadLetterQ.Write(WrapPoisonMessage(message, default), QueueTransaction.SingleMessage);
                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error processing invalid requests");
            throw;
        }
        finally
        {
            this.log.LogInformation(EventIds.DispatchingStopped, "Processing invalid requests stopped");
        }
    }
 
    /// <summary>
    /// Gets or creates the response queue for the specified message queue name.
    /// </summary>
    /// <param name="queueName">The message queue name.</param>
    /// <returns>The response queue writer.</returns>
    protected IMessageQueueWriter GetResponseQueue(MessageQueueName queueName)
    {
        return this.responseCache.GetOrAdd(queueName, (queueName, mq) => mq.CreateWriter(queueName), this.mq);
    }

    /// <summary>
    /// Creates a poison message from the specified message and exception.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>The poison message.</returns>
    protected static Message WrapPoisonMessage(in Message message, Exception? exception)
    {
        if (exception is not null)
        {
            var errorMessage = exception.Message;
            if (errorMessage.Length > Message.MaxLabelLength)
            {
                errorMessage = errorMessage[..Message.MaxLabelLength];
            }

            return new Message(Encoding.Unicode.GetBytes(exception.ToString()), JsonSerializer.SerializeToUtf8Bytes(message))
            {
                BodyType = MessageBodyType.UnicodeString,
                Label = errorMessage,
            };
        }

        return new Message(Encoding.Unicode.GetBytes(JsonSerializer.Serialize(message)))
        {
            BodyType = MessageBodyType.UnicodeString,
            Label = message.Label,
        };
    }

    /// <summary>
    /// Creates the parallel options used for parallel processing of the specified request type.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parallel options.</returns>
    protected virtual ParallelOptions CreateParallelOptions<TRequest>(CancellationToken cancellationToken)
        where TRequest : IRequest
    {
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            TaskScheduler = this.ParallelOptions.TaskScheduler,
        };

        if (this.ParallelOptions.CancellationToken != default && this.ParallelOptions.CancellationToken != CancellationToken.None)
        {
            options.CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.ParallelOptions.CancellationToken).Token;
        }

        if (this.ParallelOptions.MaxDegreeOfParallelism > 0)
        {
            options.MaxDegreeOfParallelism = this.ParallelOptions.MaxDegreeOfParallelism;
        }

        return options;
    }

    private RequestWrapper ReadRequest(in Message message, CancellationToken cancellationToken) => 
        new(this, message, cancellationToken);

    private readonly struct RequestWrapper : IDisposable
    {
        private static readonly TimeSpan MaxSupportedTimeout = TimeSpan.FromMilliseconds(0xfffffffe);

        private readonly IRequest request;
        private readonly CancellationTokenSource? timeoutCts;
        private readonly CancellationTokenSource? linkedCts;

        public RequestWrapper(QueueRequestHandler requestHandler, in Message message, CancellationToken cancellationToken)
        {
            this.request = requestHandler.Read<IRequest>(message);
            if (this.request is RequestBase mutableRequest)
            {
                var timeout = message.TimeToBeReceived;
                if (requestHandler.ReadProperties.HasFlag(MessageProperty.TimeToBeReceived) &&
                    timeout > DefaultTimeout && timeout < MaxSupportedTimeout)
                {
                    // respect non-default timeout provided by the request dispatcher

                    this.timeoutCts = new CancellationTokenSource(timeout);
                    this.linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.timeoutCts.Token);
                    mutableRequest.CancellationToken = this.linkedCts.Token;
                }
                else
                {
                    mutableRequest.CancellationToken = cancellationToken;
                }
            }
        }

        public void Dispose()
        {
            this.linkedCts?.Dispose();
            this.timeoutCts?.Dispose();
        }

        public IQuery GetQuery() => (IQuery)this.request;

        public ICommand GetCommand() => (ICommand)this.request;
    }
}