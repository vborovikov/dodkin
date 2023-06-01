namespace Dodkin
{
    /// <summary>
    /// Represents a message queue opened for sending messages.
    /// </summary>
    public interface IMessageQueueWriter : IMessageQueue
    {
        MessageQueueName Name { get; }

        /// <summary>
        /// Asks MSMQ to attempt to deliver a message.
        /// To ensure the message reached the queue you need to check acknowledgement messages sent to the <see cref="Message.AdministrationQueue"/>
        /// </summary>
        /// <param name="message">The message to try to send</param>
        /// <param name="transaction">can be NULL for no transaction, a <see cref="QueueTransaction"/>, <see cref="QueueTransaction.SingleMessage"/>, or <see cref="QueueTransaction.DTC"/>.</param>
        void Write(Message message, QueueTransaction? transaction = null);

        /// <summary>
        /// Asks MSMQ to attempt to deliver a message.
        /// To ensure the message reached the queue you need to check acknowledgement messages sent to the <see cref="Message.AdministrationQueue"/>
        /// </summary>
        /// <param name="message">The message to try to send</param>
        /// <param name="transaction">can be NULL for no transaction, a <see cref="QueueTransaction"/>, <see cref="QueueTransaction.SingleMessage"/>, or <see cref="QueueTransaction.DTC"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task WriteAsync(Message message, QueueTransaction? transaction = null, CancellationToken cancellationToken = default);
    }
}