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
    public MessageQueueName? AdministrationQueue { get; init; }

	/// <summary>
	/// The dead-letter queue for rejected or failed to handle messages.
	/// </summary>
	public MessageQueueName? DeadLetterQueue { get; init; }
}
