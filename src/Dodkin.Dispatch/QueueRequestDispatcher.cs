namespace Dodkin.Dispatch;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

public interface IQueueRequestDispatcher : IRequestDispatcher
{
    Task ExecuteAsync<TCommand>(TCommand command, TimeSpan timeout) where TCommand : ICommand;
    Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout);
}

public class QueueRequestDispatcher : QueueOperator, IQueueRequestDispatcher
{
    private static readonly TimeSpan DefaultScheduleTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger log;
    private readonly IMessageQueueWriter requestQ;
    private readonly IMessageQueueReader responseQ;
    private readonly IMessageQueueReader adminQ;

    public QueueRequestDispatcher(MessageQueueName requestQueueName, MessageEndpoint endpoint, ILogger logger)
        : this(requestQueueName, endpoint, MessageQueueFactory.Instance, logger)
    { }

    public QueueRequestDispatcher(MessageQueueName requestQueueName, MessageEndpoint endpoint,
        IMessageQueueFactory messageQueueFactory, ILogger logger) : base(endpoint)
    {
        this.log = logger;
        this.requestQ = messageQueueFactory.CreateWriter(requestQueueName);
        this.responseQ = messageQueueFactory.CreateReader(this.Endpoint.ApplicationQueue);
        this.adminQ = messageQueueFactory.CreateReader(this.Endpoint.AdministrationQueue);
    }

    protected override void Dispose(bool disposing)
    {
        this.requestQ.Dispose();
        this.responseQ.Dispose();
        this.adminQ.Dispose();
    }

    public Task ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand =>
        ExecuteWaitAsync(command, null);

    public Task ExecuteAsync<TCommand>(TCommand command, TimeSpan timeout) where TCommand : ICommand =>
        ExecuteWaitAsync(command, timeout);

    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query) =>
        RunWaitAsync(query, null);

    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout) =>
        RunWaitAsync(query, timeout);

    public async Task ExecuteAsync<TCommand>(TCommand command, DateTimeOffset at) where TCommand : ICommand
    {
        //todo: extract to new interface IRequestScheduler
        //todo: add timeout argument

        if (at <= DateTimeOffset.Now)
            throw new ArgumentOutOfRangeException(nameof(at));

        using var message = CreateMessage(command);
        message.TimeToBeReceived = DefaultScheduleTimeout;
        message.Acknowledgment = MessageAcknowledgment.FullReceive;
        message.AppSpecific = (uint)at.ToUnixTimeSeconds();

        await ExecuteWaitAsync(message, MessageClass.AckReceive, DefaultScheduleTimeout, command.CancellationToken);
    }

    private async Task ExecuteWaitAsync<TCommand>(TCommand command, TimeSpan? timeout) where TCommand : ICommand
    {
        using var message = CreateMessage(command, default, timeout);
        // determine the expected status
        var expectedAck = MessageClass.AckReachQueue;
        if (timeout is not null)
        {
            message.TimeToBeReceived = timeout.Value;
            message.Acknowledgment = MessageAcknowledgment.FullReceive;
            expectedAck = MessageClass.AckReceive;
        }

        await ExecuteWaitAsync(message, expectedAck, timeout, command.CancellationToken);
    }

    private async Task ExecuteWaitAsync(Message message, MessageClass expectedAck, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        try
        {
            // send the request
            this.requestQ.Write(message, QueueTransaction.SingleMessage);
            this.log.LogInformation(EventIds.CommandSent, "Sent command <{MessageId}> for execution", message.Id);

            // check the status
            await EnsureMessageReceivedAsync(message.Id, expectedAck, timeout, cancellationToken);
            this.log.LogInformation(EventIds.CommandConfirmed, "Confirmed command <{MessageId}> received for execution", message.Id);
        }
        catch (TimeoutException x)
        {
            this.log.LogWarning(EventIds.CommandTimedOut, x, "Timed out waiting for command <{MessageId}>", message.Id);
            throw;
        }
        catch (Exception x)
        {
            this.log.LogError(EventIds.CommandFailed, x, "Error executing command <{MessageId}>", message.Id);
            throw;
        }
    }

    private async Task<TResult> RunWaitAsync<TResult>(IQuery<TResult> query, TimeSpan? timeout)
    {
        using var message = CreateMessage(query, default, timeout);
        message.TimeToBeReceived = timeout ?? this.Timeout;
        message.Acknowledgment = MessageAcknowledgment.FullReceive;

        try
        {
            // send the request
            this.requestQ.Write(message, QueueTransaction.SingleMessage);
            this.log.LogInformation(EventIds.QuerySent, "Sent query <{MessageId}> for execution", message.Id);

            await EnsureMessageReceivedAsync(message.Id, MessageClass.AckReceive, timeout, query.CancellationToken);
            this.log.LogInformation(EventIds.QueryConfirmed, "Confirmed query <{MessageId}> received for execution", message.Id);

            // receive the response
            using var resultMsg = await this.responseQ.ReadAsync(message.Id, MessageProperties,
                timeout ?? this.Timeout, query.CancellationToken);
            this.log.LogInformation(EventIds.ResultReceived, "Received query <{MessageId}> result", message.Id);

            return Read<TResult>(resultMsg);
        }
        catch (TimeoutException x)
        {
            this.log.LogWarning(EventIds.QueryTimedOut, x, "Timed out waiting for query <{MessageId}>", message.Id);
            throw;
        }
        catch (Exception x)
        {
            this.log.LogError(EventIds.QueryFailed, x, "Error executing query <{MessageId}>", message.Id);
            throw;
        }
    }

    private async Task EnsureMessageReceivedAsync(MessageId messageId, MessageClass expectedAck, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        var ack = await this.adminQ.ReadAsync(messageId, MessageProperty.Class,
            timeout ?? this.Timeout, cancellationToken);
        this.log.LogDebug(EventIds.MessageAckNack, "Received message <{MessageId}> ack/nack: {AckClass}", messageId, ack.Class);

        if (ack.IsEmpty || ack.Class != expectedAck)
        {
            throw new TimeoutException();
        }
    }
}