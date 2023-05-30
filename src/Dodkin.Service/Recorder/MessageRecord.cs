namespace Dodkin.Service.Recorder;

record MessageRecord
{
    public const int MaxRetryCount = 3;

    public const MessageProperty RequiredProperties =
        MessageProperty.MessageId | MessageProperty.AppSpecific | MessageProperty.RespQueue;

    public const MessageProperty AllProperties =
        MessageProperty.MessageId | MessageProperty.CorrelationId |
        MessageProperty.Journal | MessageProperty.Acknowledge | MessageProperty.AppSpecific |
        MessageProperty.Label | MessageProperty.Body | MessageProperty.BodyType | MessageProperty.Extension |
        MessageProperty.RespQueue | MessageProperty.AdminQueue | MessageProperty.DeadLetterQueue;

    public MessageId MessageId { get; init; }
    public Message Message { get; init; }
    public MessageQueueName Destination { get; init; }
    public DateTimeOffset DueTime { get; init; }
    public int RetryCount { get; init; }

    public bool IsValid => !this.Message.IsEmpty && this.DueTime > DateTimeOffset.Now;

    public Message CreateMessage(MessageQueueName administrationQN, TimeSpan timeout)
    {
        var message = new Message(this.Message.Body.ToArray(), this.Message.Extension.ToArray())
        {
            CorrelationId = this.Message.CorrelationId,
            Journal = this.Message.Journal | MessageJournaling.DeadLetter,
            AppSpecific = this.Message.AppSpecific,
            Label = this.Message.Label,
            BodyType = this.Message.BodyType,
            DeadLetterQueue = this.Message.DeadLetterQueue,
            // set by Dodkin Service to gurantee delivery
            AdministrationQueue = administrationQN,
            TimeToReachQueue = timeout, // if the queue is local, the message always reaches the queue
            TimeToBeReceived = timeout,
            Acknowledgment = MessageAcknowledgment.FullReceive,
        };

        return message;
    }

    public static MessageRecord From(in Message message)
    {
        return new()
        {
            MessageId = message.Id,
            Destination = MessageQueueName.Parse(message.ResponseQueue),
            Message = message,
            DueTime = DateTimeOffset.FromUnixTimeSeconds(message.AppSpecific),
        };
    }

    public static bool Validate(in Message message)
    {
        return
            message.Id != default &&
            !string.IsNullOrWhiteSpace(message.ResponseQueue) &&
            DateTimeOffset.FromUnixTimeSeconds(message.AppSpecific) > DateTimeOffset.Now;
    }
}