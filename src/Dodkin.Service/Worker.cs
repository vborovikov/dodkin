namespace Dodkin.Service;

using Data;
using Microsoft.Extensions.Options;
using Delivery;

sealed class Worker : BackgroundService
{
    private readonly ServiceOptions options;
    private readonly IMessageQueueFactory mq;
    private readonly IMessageStore db;
    private readonly ILogger<Worker> log;
    private readonly AsyncManualResetEvent msgEvent;

    public Worker(IOptions<ServiceOptions> options,
        IMessageQueueFactory mq, IMessageStore ms,
        ILogger<Worker> log)
    {
        this.options = options.Value;
        this.mq = mq;
        this.db = ms;
        this.log = log;
        this.msgEvent = new();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            this.options.Endpoint.CreateIfNotExists(ServiceOptions.ServiceName, isTransactional: true);
        }
        catch (Exception ex)
        {
            this.log.LogError(ex, "Failed to create service message endpoint: {MessageEndpoint}.",
                this.options.Endpoint);
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.WhenAny(ReceiveMessagesAsync(stoppingToken), DeliverMessagesAsync(stoppingToken));
    }

    private async Task ReceiveMessagesAsync(CancellationToken stoppingToken)
    {
        using var serviceQ = this.mq.CreateSorter(this.options.Endpoint.ApplicationQueue);
        this.log.LogInformation("Worker started to receive messages at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            var peekMessage = await serviceQ.PeekAsync(MessageProperty.LookupId | MessageRecord.RequiredProperties,
                cancellationToken: stoppingToken);

            using var tx = new QueueTransaction();
            try
            {
                if (MessageRecord.Validate(peekMessage))
                {
                    using var message = serviceQ.Read(peekMessage.LookupId, MessageRecord.AllProperties, tx);
                    // store the message
                    await this.db.AddAsync(MessageRecord.From(message), stoppingToken);
                    // signal for delivery
                    this.msgEvent.Set();
                }
                else
                {
                    // NACK message
                    serviceQ.Reject(peekMessage.LookupId, tx);
                    this.log.LogWarning("Worker rejected message {MessageId}", peekMessage.Id);
                }
                // ACK message
                tx.Commit();
            }
            catch (Exception x)
            {
                this.log.LogError(x, "Worker failed to receive message {MessageId}", peekMessage.Id);
                // NACK message
                tx.Abort();
            }
        }
    }

    private async Task DeliverMessagesAsync(CancellationToken stoppingToken)
    {
        this.log.LogInformation("Worker started to handle messages at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            // get next due-time from the db
            var dueTime = await this.db.GetDueTimeAsync(stoppingToken) ?? (DateTimeOffset.Now + this.options.WaitPeriod);
            var waitPeriod = dueTime - DateTimeOffset.Now;
            if (waitPeriod > TimeSpan.Zero)
            {
                this.msgEvent.Reset();
                this.log.LogInformation("Waiting for {WaitPeriod} before delivering messages", waitPeriod.ToText());

                // wait for it or a new message signal
                await Task.WhenAny(Task.Delay(waitPeriod, stoppingToken), this.msgEvent.WaitAsync());
                // start again if wait period isn't over
                waitPeriod = dueTime - DateTimeOffset.Now;
                if (waitPeriod > TimeSpan.Zero)
                {
                    continue;
                }
            }

            // get the current message from the db
            var messageRecord = await this.db.GetAsync(stoppingToken);
            if (messageRecord is null)
            {
                continue;
            }

            try
            {
                if (messageRecord.RetryCount < this.options.RetryCount)
                {
                    // send it to the mq
                    using var message = messageRecord.CreateMessage(this.options.Endpoint, this.options.Timeout);
                    //todo: set ConnectorType and other properties specific to the connector app
                    using var destinationQ = this.mq.CreateWriter(messageRecord.Destination);
                    destinationQ.Write(message, destinationQ.IsTransactional ? QueueTransaction.SingleMessage : null);

                    // wait until the ACK message
                    if (this.options.Endpoint.AdministrationQueue is not null)
                    {
                        using var adminQ = this.mq.CreateReader(this.options.Endpoint.AdministrationQueue);
                        using var ack = await adminQ.ReadAsync(message.Id, MessageProperty.Class, this.options.Timeout, stoppingToken);

                        if (ack.IsEmpty || ack.Class != MessageClass.AckReceive)
                        {
                            await this.db.RetryAsync(messageRecord.MessageId, stoppingToken);
                            continue;
                        }
                    }
                }
                else
                {
                    if (MessageQueueName.TryParse(messageRecord.Message.DeadLetterQueue, out var deadLetterQN))
                    {
                        // send the message to DLQ
                        using var deadLetterQ = this.mq.CreateWriter(deadLetterQN);
                        deadLetterQ.Write(messageRecord.Message, QueueTransaction.SingleMessage);
                    }
                }

                // delete the message from the db
                await this.db.RemoveAsync(messageRecord.MessageId, stoppingToken);
            }
            catch (Exception x)
            {
                this.log.LogError(x, "Worker failed to send message to the queue {MessageQueue}", messageRecord.Destination);
                await this.db.RetryAsync(messageRecord.MessageId, stoppingToken);
            }
        }
    }
}