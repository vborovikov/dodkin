namespace Dodkin.Dispatch;

using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;
using Relay.RequestModel.Default;

public abstract class QueueRequestHandler : QueueOperator, IRequestDispatcher
{
    private const int ResponseCacheCapacity = 8;
    private const string CommandSubqueueName = "commands";
    private const string QuerySubqueueName = "queries";

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

    protected QueueRequestHandler(MessageEndpoint endpoint, ILogger logger)
        : this(endpoint, MessageQueueFactory.Instance, logger, null)
    {
    }

    protected QueueRequestHandler(MessageEndpoint endpoint, IMessageQueueFactory messageQueueFactory, ILogger logger, IRequestDispatcher? requestDispatcher = null)
        : base(endpoint)
    {
        this.mq = messageQueueFactory;
        this.log = logger;
        this.dispatcher = requestDispatcher ?? new InternalRequestDispatcher(this);
        this.responseCache = new(ResponseCacheCapacity);
    }

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

    public Task ProcessAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(
            DispatchRequestsAsync(cancellationToken),
            ProcessCommandsAsync(cancellationToken),
            ProcessQueriesAsync(cancellationToken));
    }

    private async Task DispatchRequestsAsync(CancellationToken cancellationToken)
    {
        using var appQ = this.mq.CreateSorter(this.Endpoint.ApplicationQueue);
        using var cmdSQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(CommandSubqueueName));
        using var qrySQ = new Subqueue(this.Endpoint.ApplicationQueue.GetSubqueueName(QuerySubqueueName));

        try
        {
            while (true)
            {
                using var msg = await appQ.PeekAsync(MessageProperty.LookupId | MessageProperty.Extension, null, cancellationToken);
                this.log.LogInformation(EventIds.RequestReceived, "Received request [{MessageLookupId}]", msg.LookupId);

                using var tx = new QueueTransaction();
                try
                {
                    var requestType = FindBodyType(msg);
                    if (typeof(ICommand).IsAssignableFrom(requestType))
                    {
                        appQ.Move(msg.LookupId, cmdSQ, tx);
                        this.log.LogInformation(EventIds.CommandRecognized, "Recognized command [{MessageLookupId}]", msg.LookupId);
                    }
                    else if (typeof(IQuery).IsAssignableFrom(requestType))
                    {
                        appQ.Move(msg.LookupId, qrySQ, tx);
                        this.log.LogInformation(EventIds.QueryRecognized, "Recognized query [{MessageLookupId}]", msg.LookupId);
                    }
                    else
                    {
                        appQ.Reject(msg.LookupId, tx);
                        this.log.LogWarning(EventIds.RequestRejected, "Rejected request [{MessageLookupId}]", msg.LookupId);
                    }

                    tx.Commit();
                }
                catch (Exception x) when (x is not OperationCanceledException)
                {
                    tx.Abort();
                    this.log.LogError(EventIds.RequestFailed, x, "Error processing request [{MessageLookupId}]", msg.LookupId);
                }
            }
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.ProcessingFailed, x, "Error processing requests");
            throw;
        }
    }

    private async Task ProcessCommandsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var commandQ = this.mq.CreateReader(this.Endpoint.ApplicationQueue.GetSubqueueName(CommandSubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await foreach (var message in commandQ.ReadAllAsync(MessageProperties, cancellationToken))
            {
                try
                {
                    var command = Read<ICommand>(message);
                    await ExecuteCommandAsync(command);
                    this.log.LogInformation(EventIds.CommandExecuted, "Executed command <{MessageId}>[{MessageLookupId}]",
                        message.Id, message.LookupId);
                }
                catch (Exception x) when (x is NotImplementedException || x.GetBaseException() is NotImplementedException)
                {
                    this.log.LogWarning(EventIds.CommandNotImplemented, x, "Command <{MessageId}>[{MessageLookupId}] not implemented",
                        message.Id, message.LookupId);
                }
                catch (Exception x) when (x is not OperationCanceledException)
                {
                    this.log.LogError(EventIds.CommandExecutionFailed, x, "Error executing command <{MessageId}>[{MessageLookupId}]",
                        message.Id, message.LookupId);
                    deadLetterQ.Write(WrapPoisonMessage(message, x), QueueTransaction.SingleMessage);
                }
            }
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.ProcessingFailed, x, "Error processing commands");
            throw;
        }
    }

    private async Task ProcessQueriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var queryQ = this.mq.CreateReader(this.Endpoint.ApplicationQueue.GetSubqueueName(QuerySubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await Parallel.ForEachAsync(
                queryQ.ReadAllAsync(MessageProperties, cancellationToken), cancellationToken,
                async (message, cancellationToken) =>
                {
                    try
                    {
                        var query = Read<IQuery>(message);
                        var result = await RunQueryAsync(query);
                        this.log.LogInformation(EventIds.QueryExecuted, "Executed query <{MessageId}>[{MessageLookupId}]",
                            message.Id, message.LookupId);

                        using var response = CreateMessage(result, message.Id);
                        var responseQ = this.responseCache.GetOrAdd(MessageQueueName.Parse(message.ResponseQueue),
                            (queueName, mq) => mq.CreateWriter(queueName), this.mq);
                        responseQ.Write(response, QueueTransaction.SingleMessage);
                        this.log.LogInformation(EventIds.QueryResultSent, "Sent query <{MessageId}>[{MessageLookupId}] result",
                            message.Id, message.LookupId);
                    }
                    catch (Exception x) when (x is NotImplementedException || x.GetBaseException() is NotImplementedException)
                    {
                        this.log.LogWarning(EventIds.QueryNotImplemented, x, "Query <{MessageId}>[{MessageLookupId}] not implemented",
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
            this.log.LogError(EventIds.ProcessingFailed, x, "Error processing queries");
            throw;
        }
    }

    private static Message WrapPoisonMessage(in Message message, Exception exception)
    {
        return new Message(Encoding.Unicode.GetBytes(exception.ToString()), JsonSerializer.SerializeToUtf8Bytes(message))
        {
            BodyType = MessageBodyType.UnicodeString,
            Label = exception.Message,
        };
    }
}