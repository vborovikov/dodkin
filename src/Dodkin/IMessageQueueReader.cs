namespace Dodkin
{
    /// <summary>
    /// Represents a message queue opened for peeking at or retrieving messages.
    /// </summary>
    public interface IMessageQueueReader : IDisposable
    {
        MessageQueueName Name { get; }

        /// <summary>
        /// Reads the message at the current cursor location but does not remove it from the queue.
        /// The cursor remains pointing at the current message.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="transaction">A transaction object</param>
        /// <returns>The message, which can be <c>default</c> if the operation times out.</returns>
        Message Peek(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = default, QueueTransaction? transaction = null);

        /// <summary>
        /// Reads the message at the current cursor location but does not remove it from the queue.
        /// The cursor remains pointing at the current message.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains the message received,
        /// which can be <c>default</c> if the operation times out.
        /// </returns>
        Task<Message> PeekAsync(MessageProperty properties, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the message at the current cursor location and removes it from the queue.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="transaction">A transaction object</param>
        /// <returns>The message, which can be <c>default</c> if the operation times out.</returns>
        Message Read(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = default, QueueTransaction? transaction = null);

        /// <summary>
        /// Reads the message at the current cursor location and removes it from the queue.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains the message received,
        /// which can be <c>default</c> if the operation times out.
        /// </returns>
        Task<Message> ReadAsync(MessageProperty properties, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

        IAsyncEnumerable<Message> ReadAllAsync(MessageProperty propertyFlags, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Provides extension methods for <see cref="IMessageQueueReader"/> type.
    /// </summary>
    public static class MessageQueueReaderExtensions
    {
        /// <summary>
        /// Reads the message with the given correlation ID at the current cursor location and removes it from the queue.
        /// </summary>
        /// <param name="reader">The message queue reader object.</param>
        /// <param name="correlationId">The correlation ID.</param>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains the message received,
        /// which can be <c>default</c> if the operation times out.
        /// </returns>
        public static Task<Message> ReadAsync(this IMessageQueueReader reader, MessageId correlationId, MessageProperty properties, TimeSpan? timeout = default, CancellationToken cancellationToken = default)
        {
            return ((MessageQueueReader)reader).ReadAsync(correlationId, properties, timeout, cancellationToken);
        }

    }
}