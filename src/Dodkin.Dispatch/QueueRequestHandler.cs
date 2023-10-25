namespace Dodkin.Dispatch;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Relay.RequestModel;
using Relay.RequestModel.Default;

public abstract class QueueRequestHandlerBase : QueueOperator, IRequestDispatcher
{
    private class InternalRequestDispatcher : DefaultRequestDispatcherBase
    {
        private readonly QueueRequestHandlerBase requestHandler;

        public InternalRequestDispatcher(QueueRequestHandlerBase requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        protected override object GetRequestHandler(Type requestHandlerType) => this.requestHandler;
    }

    private readonly IRequestDispatcher requestDispatcher;
    private readonly LRUCache<MessageQueueName, IMessageQueueWriter> responseCache;

    protected QueueRequestHandlerBase(MessageEndpoint endpoint) : base(endpoint)
    {
        this.requestDispatcher = new InternalRequestDispatcher(this);
        this.responseCache = new(5);
    }

    protected QueueRequestHandlerBase(MessageEndpoint endpoint, IRequestDispatcher requestDispatcher) : base(endpoint)
    {
        this.requestDispatcher = requestDispatcher;
        this.responseCache = new(5);
    }

    public abstract Task ProcessAsync(CancellationToken cancellationToken);

    protected override void Dispose(bool disposing)
    {
        this.responseCache.Dispose();
        base.Dispose(disposing);
    }

    protected sealed override Task SendMessageAsync(Message message, MessageQueueName? destinationQueue, CancellationToken cancellationToken)
    {
        if (destinationQueue is null)
            throw new ArgumentNullException(nameof(destinationQueue));

        var responseQ = this.responseCache.GetOrAdd(destinationQueue, queueName => new MessageQueueWriter(queueName));
        responseQ.Write(message, responseQ.IsTransactional ? QueueTransaction.SingleMessage : null);

        return Task.CompletedTask;
    }

    protected Task<object> RunQueryAsync(IQuery query) =>
        this.requestDispatcher.RunGenericAsync(query);

    protected Task ExecuteCommandAsync(ICommand command) =>
        this.requestDispatcher.ExecuteGenericAsync(command);

    Task<TResult> IRequestDispatcher.RunAsync<TResult>(IQuery<TResult> query) =>
        this.requestDispatcher.RunAsync(query);

    Task IRequestDispatcher.ExecuteAsync<TCommand>(TCommand command) =>
        this.requestDispatcher.ExecuteAsync(command);
}

public abstract class QueueRequestHandler : QueueRequestHandlerBase
{
    protected QueueRequestHandler(MessageEndpoint endpoint) : base(endpoint) { }

    protected QueueRequestHandler(MessageEndpoint endpoint, IRequestDispatcher requestDispatcher) : base(endpoint, requestDispatcher) { }

    public sealed override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var commandChannel = Channel.CreateUnbounded<ICommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });
        var queryChannel = Channel.CreateUnbounded<Envelope>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });

        await Task.WhenAll(
            ReceiveRequestsAsync(commandChannel, queryChannel, cancellationToken),
            HandleCommandsAsync(commandChannel, cancellationToken),
            HandleQueriesAsync(queryChannel, cancellationToken));
    }

    private async Task ReceiveRequestsAsync(ChannelWriter<ICommand> commandWriter, ChannelWriter<Envelope> queryWriter, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var message = await ReceiveRequestAsync(cancellationToken);
                if (message.Request is RequestBase nativeRequest)
                {
                    nativeRequest.CancellationToken = cancellationToken;
                }
                if (message.Request is ICommand command)
                {
                    await commandWriter.WriteAsync(command, cancellationToken);
                }
                else if (message.Request is not null)
                {
                    await queryWriter.WriteAsync(message, cancellationToken);
                }
            }
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            commandWriter.Complete(x);
            queryWriter.Complete(x);
        }
        finally
        {
            commandWriter.TryComplete();
            queryWriter.TryComplete();
        }
    }

    private async Task HandleCommandsAsync(ChannelReader<ICommand> commandReader, CancellationToken cancellationToken)
    {
        await foreach (var command in commandReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ExecuteCommandAsync(command);
            }
            catch (Exception x) when (x is not OperationCanceledException)
            {
                continue;
            }
        }
    }

    private async Task HandleQueriesAsync(ChannelReader<Envelope> queryReader, CancellationToken cancellationToken)
    {
        await foreach (var query in queryReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var result = await RunQueryAsync((IQuery)query.Request!);
                await SendResultAsync(result, query, cancellationToken);
            }
            catch (Exception x) when (x is not OperationCanceledException)
            {
                continue;
            }
        }
    }
}