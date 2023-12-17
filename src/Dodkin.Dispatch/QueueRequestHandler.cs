namespace Dodkin.Dispatch;

using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;
using Relay.RequestModel.Default;

public class QueueRequestHandler : QueueOperator, IRequestDispatcher
{
    private const int ResponseCacheCapacity = 8;
    private const string CommandSubqueueName = "commands";
    private const string QuerySubqueueName = "queries";
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

    public QueueRequestHandler(IRequestDispatcher requestDispatcher, MessageEndpoint endpoint, IMessageQueueFactory messageQueueFactory, ILogger logger)
        : this(endpoint, messageQueueFactory, logger, requestDispatcher) { }

    protected QueueRequestHandler(MessageEndpoint endpoint, ILogger logger)
        : this(endpoint, MessageQueueFactory.Instance, logger, null)
    {
    }

    protected QueueRequestHandler(MessageEndpoint endpoint, IMessageQueueFactory messageQueueFactory, ILogger logger, IRequestDispatcher? requestDispatcher = null)
        : base(endpoint, logger)
    {
        this.mq = messageQueueFactory;
        this.log = logger;
        this.dispatcher = requestDispatcher ?? new InternalRequestDispatcher(this);
        this.responseCache = new(ResponseCacheCapacity);
    }

    protected MessageProperty PeekProperties { get; init; } = MessageProperty.None;

    public ParallelOptions ParallelOptions { get; init; } = new();

    protected override void Dispose(bool disposing)
    {
        this.responseCache.Dispose();
    }

    protected Task<object> RunQueryAsync(IQuery query) =>
        this.dispatcher.RunGenericAsync(query);

    protected Task ExecuteCommandAsync(ICommand command) =>
        this.dispatcher.ExecuteGenericAsync(command);

    Task<TResult> IRequestDispatcher.RunAsync<TResult>(IQuery<TResult> query) =>
        this.dispatcher.RunAsync(query);

    Task IRequestDispatcher.ExecuteAsync<TCommand>(TCommand command) =>
        this.dispatcher.ExecuteAsync(command);

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

    protected virtual ICollection<Task> GetProcessingTasks(CancellationToken cancellationToken)
    {
        return new List<Task>
        {
            DispatchRequestsAsync(cancellationToken),
            ProcessCommandsAsync(cancellationToken),
            ProcessQueriesAsync(cancellationToken),
        };
    }

    private async Task DispatchRequestsAsync(CancellationToken cancellationToken)
    {
        using var appQ = this.mq.CreateSorter(this.Endpoint.ApplicationQueue);
        using var cmdSQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(CommandSubqueueName));
        using var qrySQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(QuerySubqueueName));
        using var reqSQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(RequestSubqueueName));

        try
        {
            while (true)
            {
                using var msg = await appQ.PeekAsync(MessageProperty.LookupId | MessageProperty.Extension | this.PeekProperties, null, cancellationToken);
                this.log.LogInformation(EventIds.MessageReceived, "Received message [{MessageLookupId}]", msg.LookupId);

                using var tx = new QueueTransaction();
                try
                {
                    if (CanDispatchRequest(msg))
                    {
                        appQ.Move(msg.LookupId, reqSQ, tx);
                        this.log.LogInformation(EventIds.RequestDispatched, "Dispatched request [{MessageLookupId}]", msg.LookupId);
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
            }
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error dispatching messages");
            throw;
        }
    }

    protected virtual bool CanDispatchRequest(in Message message) => false;

    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var commandQ = this.mq.CreateReader(this.Endpoint.ApplicationQueue.GetSubqueueName(CommandSubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await Parallel.ForEachAsync(commandQ.ReadAllAsync(MessageProperties, cancellationToken), CreateParallelOptions<ICommand>(cancellationToken),
                async (message, cancellationToken) =>
                {
                    try
                    {
                        var command = Read<ICommand>(message);
                        if (command is RequestBase request)
                        {
                            request.CancellationToken = cancellationToken;
                        }
                        await ExecuteCommandAsync(command);
                        this.log.LogInformation(EventIds.CommandExecuted, "Executed command <{MessageId}>[{MessageLookupId}]",
                            message.Id, message.LookupId);
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
                });
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error processing commands");
            throw;
        }
    }

    private async Task ProcessQueriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var queryQ = this.mq.CreateReader(this.Endpoint.ApplicationQueue.GetSubqueueName(QuerySubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await Parallel.ForEachAsync(queryQ.ReadAllAsync(MessageProperties, cancellationToken), CreateParallelOptions<IQuery>(cancellationToken),
                async (message, cancellationToken) =>
                {
                    try
                    {
                        var query = Read<IQuery>(message);
                        if (query is RequestBase request)
                        {
                            request.CancellationToken = cancellationToken;
                        }
                        var result = await RunQueryAsync(query);
                        this.log.LogInformation(EventIds.QueryExecuted, "Executed query <{MessageId}>[{MessageLookupId}]",
                            message.Id, message.LookupId);

                        using var response = CreateMessage(result, message.Id);
                        var responseQ = GetResponseQueue(MessageQueueName.Parse(message.ResponseQueue));
                        responseQ.Write(response, QueueTransaction.SingleMessage);
                        this.log.LogInformation(EventIds.QueryResultSent, "Sent query <{MessageId}>[{MessageLookupId}] result",
                            message.Id, message.LookupId);
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
                });
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.DispatchingFailed, x, "Error processing queries");
            throw;
        }
    }

    protected IMessageQueueWriter GetResponseQueue(MessageQueueName queueName)
    {
        return this.responseCache.GetOrAdd(queueName, (queueName, mq) => mq.CreateWriter(queueName), this.mq);
    }

    protected static Message WrapPoisonMessage(in Message message, Exception exception)
    {
        var errorMessage = exception.Message;
        if (errorMessage.Length > 249)
        {
            errorMessage = errorMessage[..249];
        }

        return new Message(Encoding.Unicode.GetBytes(exception.ToString()), JsonSerializer.SerializeToUtf8Bytes(message))
        {
            BodyType = MessageBodyType.UnicodeString,
            Label = errorMessage,
        };
    }

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
}