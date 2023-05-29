namespace Dodkin
{
    /// <summary>
    /// Represents a message queue and/or subqueue opened for sorting messages.
    /// </summary>
    public interface IMessageQueueSorter : IMessageQueueReader
    {
        /// <summary>
        /// Reads the message with the given <paramref name="lookupId"/>.
        /// </summary>
        /// <param name="lookupId">The message lookup id.</param>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="transaction">The transaction object.</param>
        /// <exception cref="MessageQueueException">The message was not found or there was an error.</exception>
        /// <returns>The message.</returns>
        Message Read(MessageLookupId lookupId, MessageProperty properties = MessageProperty.All, QueueTransaction? transaction = null);

        /// <summary>
        /// Moves the message with the given <paramref name="lookupId"/>.
        /// </summary>
        /// <param name="lookupId">The message lookup id.</param>
        /// <param name="transaction">The transaction object.</param>
        /// <exception cref="MessageQueueException">The message was not found or there was an error.</exception>
        void Move(MessageLookupId lookupId, QueueTransaction? transaction = null);

        /// <summary>
        /// Rejects the message with the given <paramref name="lookupId"/>.
        /// </summary>
        /// <param name="lookupId">The message lookup id.</param>
        /// <exception cref="MessageQueueException">The message was not found or there was an error.</exception>
        void Reject(MessageLookupId lookupId);
    }
}