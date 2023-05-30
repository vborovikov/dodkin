namespace Dodkin.Service.Recorder;

record MessageRecord
{
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