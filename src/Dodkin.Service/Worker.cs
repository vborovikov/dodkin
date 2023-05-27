namespace Dodkin.Service;

using Data;
using Microsoft.Extensions.Options;
using Recorder;

sealed class Worker : BackgroundService
{
    private readonly ServiceOptions options;
    private readonly IMessageQueueFactory mq;
    private readonly IMessageStore ms;
    private readonly ILogger<Worker> log;
    private readonly AsyncManualResetEvent msgEvent;

    public Worker(IOptions<ServiceOptions> options,
        IMessageQueueFactory mq, IMessageStore ms,
        ILogger<Worker> log)
    {
        this.options = options.Value;
        this.mq = mq;
        this.ms = ms;
        this.log = log;
        this.msgEvent = new();
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var applicationQueueName = MessageQueueName.Parse(this.options.ApplicationQueue);
        try
        {
            if (!MessageQueue.Exists(applicationQueueName))
            {
                MessageQueue.TryCreate(applicationQueueName, isTransactional: true);
            }
        }
        catch (Exception ex)
        {
            this.log.LogError(ex, $"Failed to create message queue {applicationQueueName}.");
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
        var appQueue = this.mq.CreateReader(MessageQueueName.Parse(this.options.ApplicationQueue));
        this.log.LogInformation("Worker started to receive messages at: {time}", DateTimeOffset.Now);

        await foreach (var message in appQueue.ReadAllAsync(MessageProperty.All, stoppingToken))
        {
            //todo: validate the message
            // store the message
            await this.ms.AddAsync(MessageRecord.From(message), stoppingToken);
            // signal for delivery
            this.msgEvent.Set();
            //todo: send ACK or NACK to the sender
        }
    }

    private async Task DeliverMessagesAsync(CancellationToken stoppingToken)
    {
        this.log.LogInformation("Worker started to handle messages at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            // get next due-time from the db
            var dueTime = await this.ms.GetDueTimeAsync(stoppingToken) ?? DateTimeOffset.Now.AddDays(1);
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
            var messageRecord = await this.ms.GetAsync(stoppingToken);
            if (messageRecord is null)
            {
                continue;
            }

            // send it to the mq
            using var destQueue = this.mq.CreateWriter(messageRecord.Destination);
            await destQueue.WriteAsync(messageRecord.Message, null, stoppingToken);

            // delete the message from the db
            await this.ms.RemoveAsync(messageRecord.MessageId, stoppingToken);
        }
    }
}