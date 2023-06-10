namespace Dodkin;

using Interop;

/// <summary>
/// Represents an implicitly created logical partition of a message queue.
/// </summary>
public sealed class Subqueue : MessageQueue
{
    private readonly QueueConnection cnn;

    /// <summary>
    /// Initializes a new instance of the <see cref="Subqueue"/> class.
    /// </summary>
    public Subqueue(MessageQueueName queueName)
    {
        if (!queueName.IsSubqueue)
            throw new ArgumentException("The queue name does not appear to be a subqueue name.", nameof(queueName));

        this.cnn = new(queueName, QueueAccessMode.Move, QueueShareMode.Shared);
    }

    /// <inheritdoc/>
    public override MessageQueueName Name => this.cnn.QueueName;
    /// <inheritdoc/>
    public override bool IsTransactional => this.cnn.IsTransactional;
    /// <inheritdoc/>
    internal override QueueHandle Handle => this.cnn.ReadHandle;

    /// <inheritdoc/>
    public override void Dispose()
    {
        this.cnn.Dispose();
    }
}
