namespace Dodkin.Dispatch;

using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.RequestModel;
using MsmqMessage = Dodkin.Message;

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

    protected sealed override Task SendMessageAsync(MsmqMessage message, MessageQueueName? destinationQueue, CancellationToken cancellationToken)
    {
        //todo: use transaction if needed
        this.requestQ.Write(message, null);
        return Task.CompletedTask;
    }

    private async Task<TResult> RunWaitAsync<TResult>(IQuery<TResult> query, TimeSpan? timeout)
    {
        var messageId = await SendRequestAsync(query, query.CancellationToken);
        var result = await ReceiveResultAsync<TResult>(messageId, timeout, query.CancellationToken);
        return result;
    }
}