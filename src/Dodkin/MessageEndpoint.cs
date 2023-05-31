namespace Dodkin;

/// <summary>
/// Configuration settings for message queue listener.
/// </summary>
public record MessageEndpoint
{
    /// <summary>
    /// The application queue for incoming messages.
    /// </summary>
    public required MessageQueueName ApplicationQueue { get; init; }

    /// <summary>
    /// The application subqueue for invalid messages.
    /// </summary>
    public MessageQueueName? InvalidMessageQueue { get; init; }

    /// <summary>
    /// The administration queue for ACK or NACK messages.
    /// </summary>
    public required MessageQueueName AdministrationQueue { get; init; }

    /// <summary>
    /// The dead-letter queue for rejected or failed to handle messages.
    /// </summary>
    public required MessageQueueName DeadLetterQueue { get; init; }

    /// <summary>
	/// Creates a new instance of <see cref="MessageEndpoint"/> from a queue name.
	/// </summary>
	/// <param name="queueName">The queue name.</param>
	/// <returns>A new instance of <see cref="MessageEndpoint"/>.</returns>
	public static MessageEndpoint FromName(string queueName)
    {
        return new MessageEndpoint
        {
            ApplicationQueue = new DirectFormatName(queueName, MessageQueueName.LocalComputer, isPrivate: true),
            InvalidMessageQueue = new DirectFormatName(queueName + ";invalid", MessageQueueName.LocalComputer, isPrivate: true),
            AdministrationQueue = new DirectFormatName(queueName + "-admin", MessageQueueName.LocalComputer, isPrivate: true),
            DeadLetterQueue = new DirectFormatName(queueName + "-dlq", MessageQueueName.LocalComputer, isPrivate: true),
        };
    }

	/// <summary>
	/// Creates the message queues if they do not exist.
	/// </summary>
	/// <param name="queueLabel">The queue label.</param>
	/// <param name="isTransactional">Indicates whether the application queue is transactional.</param>
	/// <exception cref="MessageQueueException">The message queue was not created.</exception>
    public void CreateIfNotExists(string queueLabel, bool isTransactional = false)
    {
        if (!MessageQueue.Exists(this.ApplicationQueue))
        {
            MessageQueue.Create(this.ApplicationQueue,
                isTransactional: isTransactional, hasJournal: true, label: queueLabel);
        }

        if (!MessageQueue.Exists(this.AdministrationQueue))
        {
            MessageQueue.Create(this.AdministrationQueue,
                isTransactional: false, hasJournal: false, label: queueLabel);
        }

        if (!MessageQueue.Exists(this.DeadLetterQueue))
        {
            MessageQueue.Create(this.DeadLetterQueue,
                isTransactional: true, hasJournal: false, label: queueLabel);
        }
    }
}
