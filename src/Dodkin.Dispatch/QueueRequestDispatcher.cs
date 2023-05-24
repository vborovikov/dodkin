namespace Dodkin.Dispatch;

using System;
using System.Threading.Tasks;
using Relay.RequestModel;

public interface IQueueRequestDispatcher : IRequestDispatcher
{
    Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout);
}

public class QueueRequestDispatcher : QueueOperator, IQueueRequestDispatcher
{
    public QueueRequestDispatcher(string inputQueuePath, string outputQueuePath, TimeSpan? timeout = null)
        : base(inputQueuePath, outputQueuePath, timeout) { }

    public Task ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        //todo: use QueueCommand.RequiresAcknowledgment
        return SendRequestAsync(command, command.CancellationToken);
    }

    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query) => RunWaitAsync(query, null);

    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout) => RunWaitAsync(query, timeout);

    private async Task<TResult> RunWaitAsync<TResult>(IQuery<TResult> query, TimeSpan? timeout)
    {
        var messageId = await SendRequestAsync(query, query.CancellationToken);
        var result = await ReceiveResultAsync<TResult>(messageId, timeout, query.CancellationToken);
        return result;
    }
}