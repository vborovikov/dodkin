namespace Dodkin.Dispatch;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

/// <summary>
/// Represents a queue operator that dispatches requests to the appropriate handlers.
/// </summary>
public interface IQueueRequestDispatcher : IRequestDispatcher
{
    /// <summary>
    /// Executes a command asynchronously with a timeout.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ExecuteAsync<TCommand>(TCommand command, TimeSpan timeout) where TCommand : ICommand;

    /// <summary>
    /// Executes a query asynchronously and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the query.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
    Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout);
}

/// <summary>
/// Represents a queue operator that schedules commands to the appropriate handlers.
/// </summary>
public interface IQueueRequestScheduler : IQueueRequestDispatcher, IRequestScheduler
{
    /// <summary>
    /// Schedules a command to be executed at a specific time.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="at">The time at which the command should be executed.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ScheduleAsync<TCommand>(TCommand command, DateTimeOffset at, TimeSpan timeout) where TCommand : ICommand;
}

/// <summary>
/// Represents a queue operator that dispatches or schedules requests to the appropriate handlers.
/// </summary>
public class QueueRequestDispatcher : QueueOperator, IQueueRequestDispatcher, IQueueRequestScheduler
{
    private static readonly TimeSpan DefaultScheduleTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger log;
    private readonly IMessageQueueWriter requestQ;
    private readonly IMessageQueueReader responseQ;
    private readonly IMessageQueueReader adminQ;

    /// <summary>
    /// Creates a new instance of the <see cref="QueueRequestDispatcher"/> class.
    /// </summary>
    /// <param name="requestQueueName">The name of the queue used to dispatch requests.</param>
    /// <param name="endpoint">The endpoint describing the queues used by the dispatcher.</param>
    /// <param name="logger">The logger.</param>
    public QueueRequestDispatcher(MessageQueueName requestQueueName, MessageEndpoint endpoint, ILogger logger)
        : this(requestQueueName, endpoint, MessageQueueFactory.Instance, logger)
    { }

    /// <summary>
    /// Creates a new instance of the <see cref="QueueRequestDispatcher"/> class.
    /// </summary>
    /// <param name="requestQueueName">The name of the queue used to dispatch requests.</param>
    /// <param name="endpoint">The endpoint describing the queues used by the dispatcher.</param>
    /// <param name="messageQueueFactory">The message queue factory.</param>
    /// <param name="logger">The logger.</param>
    public QueueRequestDispatcher(MessageQueueName requestQueueName, MessageEndpoint endpoint,
        IMessageQueueFactory messageQueueFactory, ILogger logger) : base(endpoint, logger)
    {
        this.log = logger;
        this.requestQ = messageQueueFactory.CreateWriter(requestQueueName);
        this.responseQ = messageQueueFactory.CreateReader(this.Endpoint.ApplicationQueue);
        this.adminQ = messageQueueFactory.CreateReader(this.Endpoint.AdministrationQueue);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        this.requestQ.Dispose();
        this.responseQ.Dispose();
        this.adminQ.Dispose();
    }

    /// <inheritdoc />
    public Task ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand =>
        ExecuteWaitAsync(command, null);

    /// <inheritdoc />
    public Task ExecuteAsync<TCommand>(TCommand command, TimeSpan timeout) where TCommand : ICommand =>
        ExecuteWaitAsync(command, timeout);

    /// <inheritdoc />
    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query) =>
        RunWaitAsync(query, null);

    /// <inheritdoc />
    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout) =>
        RunWaitAsync(query, timeout);

    /// <inheritdoc />
    public Task ScheduleAsync<TCommand>(TCommand command, DateTimeOffset at) where TCommand : ICommand
    {
        return ScheduleAsync(command, at, DefaultScheduleTimeout);
    }

    /// <inheritdoc />
    public async Task ScheduleAsync<TCommand>(TCommand command, DateTimeOffset at, TimeSpan timeout) where TCommand : ICommand
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(at, DateTimeOffset.Now);
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

        using var message = CreateMessage(command);
        message.TimeToBeReceived = timeout;
        message.Acknowledgment = MessageAcknowledgment.FullReceive;
        message.AppSpecific = (uint)at.ToUnixTimeSeconds();

        if (message.Body.Length <= 110)
        {
            // if the message body is small then add it to the label

            var label = string.Concat(message.Label, Encoding.UTF8.GetString(message.Body));
            if (label.Length > Message.MaxLabelLength)
            {
                label = label[..Message.MaxLabelLength];
            }

            message.Label = label;
        }

        await ExecuteWaitAsync(message, MessageClass.AckReceive, timeout, command.CancellationToken);
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
            this.log.LogInformation(EventIds.CommandTimedOut, x, "Timed out waiting for command <{MessageId}>", message.Id);
            throw;
        }
        catch (Exception x) when (x is not OperationCanceledException)
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
            this.log.LogInformation(EventIds.QueryTimedOut, x, "Timed out waiting for query <{MessageId}>", message.Id);
            throw;
        }
        catch (Exception x) when (x is not OperationCanceledException)
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
            throw new TimeoutException($"Timed out based on message <{messageId}> ack/nack: {ack.Class}");
        }
    }
}