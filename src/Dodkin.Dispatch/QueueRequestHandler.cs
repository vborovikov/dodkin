namespace Dodkin.Dispatch;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Relay.RequestModel;
using Relay.RequestModel.Default;
using MsmqMessage = Dodkin.Message;

public abstract class QueueRequestHandler : QueueOperator, IRequestDispatcher
{
    private class InternalRequestDispatcher : DefaultRequestDispatcherBase
    {
        private readonly QueueRequestHandler requestHandler;

        public InternalRequestDispatcher(QueueRequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }

        protected override object GetRequestHandler(Type requestHandlerType) => this.requestHandler;
    }

    private readonly IRequestDispatcher requestDispatcher;
    private readonly LRUCache<MessageQueueName, IMessageQueueWriter> responseCache;

    protected QueueRequestHandler(MessageEndpoint endpoint) : base(endpoint)
    {
        this.requestDispatcher = new InternalRequestDispatcher(this);
        this.responseCache = new(5);
    }

    protected override void Dispose(bool disposing)
    {
        this.responseCache.Dispose();
        base.Dispose(disposing);
    }

    protected sealed override Task SendMessageAsync(MsmqMessage message, MessageQueueName? destinationQueue, CancellationToken cancellationToken)
    {
        if (destinationQueue is null)
            throw new ArgumentNullException(nameof(destinationQueue));

        var responseQ = this.responseCache.GetOrAdd(destinationQueue, queueName => new MessageQueueWriter(queueName));
        responseQ.Write(message, responseQ.IsTransactional ? QueueTransaction.SingleMessage : null);

        return Task.CompletedTask;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var commandChannel = Channel.CreateUnbounded<ICommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });
        var queryChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });

        await Task.WhenAll(
            ReceiveRequestsAsync(commandChannel, queryChannel, cancellationToken),
            HandleCommandsAsync(commandChannel, cancellationToken),
            HandleQueriesAsync(queryChannel, cancellationToken));
    }

    private async Task ReceiveRequestsAsync(ChannelWriter<ICommand> commandWriter, ChannelWriter<Message> queryWriter, CancellationToken cancellationToken)
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception x)
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
                await this.requestDispatcher.ExecuteGenericAsync(command);
            }
            catch (Exception x) when (x is not OperationCanceledException)
            {
                continue;
            }
        }
    }

    private async Task HandleQueriesAsync(ChannelReader<Message> queryReader, CancellationToken cancellationToken)
    {
        await foreach (var query in queryReader.ReadAllAsync(cancellationToken))
        {
            try
            {
                var result = await this.requestDispatcher.RunGenericAsync((IQuery)query.Request!);
                await SendResultAsync(result, query, cancellationToken);
            }
            catch (Exception x) when (x is not OperationCanceledException)
            {
                continue;
            }
        }
    }

    Task<TResult> IRequestDispatcher.RunAsync<TResult>(IQuery<TResult> query) =>
        this.requestDispatcher.RunAsync(query);

    Task IRequestDispatcher.ExecuteAsync<TCommand>(TCommand command) =>
        this.requestDispatcher.ExecuteAsync(command);
}