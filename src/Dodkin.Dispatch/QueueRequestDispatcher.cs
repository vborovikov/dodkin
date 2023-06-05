namespace Dodkin.Dispatch;

using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.RequestModel;

public interface IQueueRequestDispatcher : IRequestDispatcher
{
    Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout);
}

public class QueueRequestDispatcher : QueueOperator, IQueueRequestDispatcher
{
    private readonly IMessageQueueWriter requestQ;

    public QueueRequestDispatcher(MessageQueueName requestQueueName, MessageEndpoint endpoint, TimeSpan? timeout = null)
        : base(endpoint, timeout)
    {
        this.requestQ = new MessageQueueWriter(requestQueueName);
    }

    public Task ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        //todo: use QueueCommand.RequiresAcknowledgment
        return SendRequestAsync(command, command.CancellationToken);
    }

    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query) => RunWaitAsync(query, null);

    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout) => RunWaitAsync(query, timeout);

    protected sealed override Task SendMessageAsync(Message message, MessageQueueName? destinationQueue, CancellationToken cancellationToken)
    {
        this.requestQ.Write(message, this.requestQ.IsTransactional ? QueueTransaction.SingleMessage : null);
        return Task.CompletedTask;
    }

    private async Task<TResult> RunWaitAsync<TResult>(IQuery<TResult> query, TimeSpan? timeout)
    {
        var messageId = await SendRequestAsync(query, query.CancellationToken);
        var result = await ReceiveResultAsync<TResult>(messageId, timeout, query.CancellationToken);
        return result;
    }
}