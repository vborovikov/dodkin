namespace Dodkin.Service.Recorder;

record MessageRecord
{
    public MessageId MessageId { get; init; }
    public Message Message { get; init; }
    public MessageQueueName Destination { get; init; }
    public DateTimeOffset DueTime { get; init; }
    public int RetryCount { get; init; }

    public bool IsValid => this.DueTime > DateTimeOffset.Now;

    public static MessageRecord From(in Message message)
    {
        return new()
        {
            MessageId = message.Id,
            Destination = MessageQueueName.FromPathName(message.ResponseQueue),
            Message = message,
            DueTime = DateTimeOffset.FromUnixTimeSeconds(message.AppSpecific),
        };
    }
}