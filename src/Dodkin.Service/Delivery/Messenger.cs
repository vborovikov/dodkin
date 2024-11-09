namespace Dodkin.Service.Delivery;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Data;
using Dispatch;
using Microsoft.Extensions.Options;
using Relay.RequestModel;

sealed class Messenger : QueueRequestHandler
{
    private readonly ServiceOptions options;
    private readonly IMessageQueueFactory mq;
    private readonly IMessageStore db;
    private readonly ILogger<Messenger> log;
    private readonly AsyncManualResetEvent msgEvent;

    public Messenger(IOptions<ServiceOptions> options,
        IMessageQueueFactory messageQueueFactory, IMessageStore messageStore,
        IRequestDispatcher requestDispatcher, ILogger<Messenger> logger)
        : base(requestDispatcher, options.Value.Endpoint, messageQueueFactory, logger)
    {
        this.PeekProperties = MessageRecord.RequiredProperties;

        this.options = options.Value;
        this.mq = messageQueueFactory;
        this.db = messageStore;
        this.log = logger;
        this.msgEvent = new();
    }

    protected override ICollection<Task> GetProcessingTasks(CancellationToken cancellationToken)
    {
        var tasks = base.GetProcessingTasks(cancellationToken);

        tasks.Add(ReceiveMessagesAsync(cancellationToken));
        tasks.Add(DeliverMessagesAsync(cancellationToken));

        return tasks;
    }

    protected override bool CanDispatchRequest(in Message message)
    {
        return MessageRecord.Validate(message);
    }

    private async Task ReceiveMessagesAsync(CancellationToken stoppingToken)
    {
        try
        {
            this.log.LogInformation(EventIds.Receiving, "Started receiving messges");

            using var serviceQ = this.mq.CreateReader(this.Endpoint.ApplicationQueue.GetSubqueueName(RequestSubqueueName));
            using var deadLetterQ = this.mq.CreateWriter(this.Endpoint.DeadLetterQueue);

            await Parallel.ForEachAsync(serviceQ.ReadAllAsync(MessageRecord.AllProperties, stoppingToken), CreateParallelOptions<ICommand>(stoppingToken),
                async (message, cancellationToken) =>
                {
                    try
                    {
                        this.log.LogInformation(EventIds.Receiving, "Received message <{MessageId}>", message.Id);

                        // store the message
                        var messageRecord = MessageRecord.From(message);
                        await this.db.AddAsync(messageRecord, cancellationToken);

                        this.log.LogInformation(EventIds.Receiving, "Stored message <{MessageId}> (to be sent at {DueTime})",
                            message.Id, messageRecord.DueTime.ToLocalTime());

                        // signal for delivery
                        this.msgEvent.Set();
                    }
                    catch (Exception x) when (x is not OperationCanceledException)
                    {
                        this.log.LogError(EventIds.Receiving, x, "Failed processing message <{MessageId}>", message.Id);
                        deadLetterQ.Write(WrapPoisonMessage(message, x), QueueTransaction.SingleMessage);

                        TrySendBack(message, message.Id);
                    }
                });
        }
        catch (Exception x) when (x is not OperationCanceledException ocx || ocx.CancellationToken != stoppingToken)
        {
            this.log.LogError(EventIds.Receiving, x, "Failed receiving messages");
            throw;
        }
        finally
        {
            this.log.LogInformation(EventIds.Receiving, "Stopped receiving messages");
        }
    }

    private async Task DeliverMessagesAsync(CancellationToken stoppingToken)
    {
#if DEBUG
        if (this.options.PurgeOnStart)
        {
            await this.db.PurgeAsync(stoppingToken);
            this.log.LogInformation(EventIds.Sending, "Purged message store");
        }
#endif

        try
        {
            this.log.LogInformation(EventIds.Sending, "Started sending messages");

            while (true)
            {
                // get the current message from the db
                var messageRecord = await this.db.GetAsync(stoppingToken);
                if (messageRecord is null)
                {
                    this.log.LogInformation(EventIds.Sending, "No message to send");
                }

                // get the wait period
                var dueTime = messageRecord?.DueTime ?? (DateTimeOffset.Now + this.options.WaitPeriod);
                var waitPeriod = dueTime - DateTimeOffset.Now;
                if (waitPeriod > TimeSpan.Zero)
                {
                    this.log.LogInformation(EventIds.Sending, "Waiting for {WaitPeriod} before sending messages ({dueTime})",
                        waitPeriod.ToText(), dueTime.ToLocalTime());

                    // wait for it or a new message signal
                    await Task.WhenAny(Task.Delay(waitPeriod, stoppingToken), this.msgEvent.WaitAsync());
                    // reset the signal here
                    this.msgEvent.Reset();

                    // start again if wait period isn't over
                    waitPeriod = dueTime - DateTimeOffset.Now;
                    if (waitPeriod > TimeSpan.Zero)
                    {
                        // time to check the db again
                        continue;
                    }
                }

                if (messageRecord is null)
                {
                    // we waited the whole wait period, check the db again
                    continue;
                }

                try
                {
                    if (messageRecord.RetryCount < this.options.RetryCount)
                    {
                        this.log.LogInformation(EventIds.Sending, "Sending message <{MessageId}>, retry {RetryCount}/{MaxRetryCount}",
                            messageRecord.MessageId, messageRecord.RetryCount + 1, this.options.RetryCount);

                        // send it to the mq
                        using var message = messageRecord.CreateMessage(this.Endpoint, this.options.Timeout);
                        //todo: set ConnectorType and other properties specific to the connector app
                        var destinationQ = GetResponseQueue(messageRecord.Destination);
                        destinationQ.Write(message, destinationQ.IsTransactional ? QueueTransaction.SingleMessage : null);

                        this.log.LogInformation(EventIds.Sending, "Sent message <{MessageId}> to \"{Destination}\"",
                            messageRecord.MessageId, messageRecord.Destination);

                        // wait until the ACK message
                        if (this.Endpoint.AdministrationQueue is not null)
                        {
                            this.log.LogDebug(EventIds.Sending, "Waiting for ACK message <{MessageId}>", messageRecord.MessageId);

                            using var adminQ = this.mq.CreateReader(this.Endpoint.AdministrationQueue);
                            using var ack = await adminQ.ReadAsync(message.Id, MessageProperty.Class, this.options.Timeout, stoppingToken);

                            if (ack.IsEmpty || ack.Class != MessageClass.AckReceive)
                            {
                                await this.db.RetryAsync(messageRecord.MessageId, stoppingToken);
                                this.log.LogWarning(EventIds.Sending, "Failed to receive ACK message <{MessageId}>, retrying", messageRecord.MessageId);
                                continue;
                            }

                            this.log.LogInformation(EventIds.Sending, "Received ACK message <{MessageId}>", messageRecord.MessageId);
                        }
                    }
                    else
                    {
                        this.log.LogWarning(EventIds.Sending, "Failed to send message <{MessageId}> to \"{Destination}\", too many retries",
                            messageRecord.MessageId, messageRecord.Destination);

                        TrySendBack(messageRecord.Message, messageRecord.MessageId);
                    }

                    // delete the message from the db
                    await this.db.RemoveAsync(messageRecord.MessageId, stoppingToken);
                    this.log.LogDebug(EventIds.Sending, "Finished processing message <{MessageId}>", messageRecord.MessageId);
                }
                catch (Exception x) when (x is not OperationCanceledException)
                {
                    this.log.LogError(EventIds.Sending, x, "Failed to send message to the queue \"{MessageQueue}\"", messageRecord.Destination);
                    await this.db.RetryAsync(messageRecord.MessageId, stoppingToken);
                }
            }
        }
        catch (Exception x) when (x is not OperationCanceledException ocx ||
            (ocx.CancellationToken != default && ocx.CancellationToken != stoppingToken))
        {
            this.log.LogError(EventIds.Sending, x, "Failed sending messages");
            throw;
        }
        finally
        {
            this.log.LogInformation(EventIds.Sending, "Stopped sending messages");
        }
    }

    private bool TrySendBack(in Message message, in MessageId originalMessageId)
    {
        if (MessageQueueName.TryParse(message.DeadLetterQueue, out var deadLetterQN))
        {
            // send the message to DLQ
            this.log.LogInformation(EventIds.Sending, "Sending message <{MessageId}> to DLQ \"{DeadLetterQueue}\"",
                originalMessageId, deadLetterQN);

            var deadLetterQ = GetResponseQueue(deadLetterQN);
            deadLetterQ.Write(message, QueueTransaction.SingleMessage);

            return true;
        }

        return false;
    }
}
