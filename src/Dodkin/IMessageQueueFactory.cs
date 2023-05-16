namespace Dodkin
{
    /// <summary>
    /// Defines methods for creating message queue writer, reader, sorter, and cursor instances.
    /// </summary>
    public interface IMessageQueueFactory
    {
        /// <summary>
        /// Creates a new instance of a message queue writer for the specified queue name.
        /// The writer can be used to send messages to the queue.
        /// </summary>
        /// <param name="queueName">The name of the message queue to create.</param>
        /// <returns>A new instance of a message queue writer.</returns>
        IMessageQueueWriter CreateWriter(MessageQueueName queueName);

        /// <summary>
        /// Creates a new instance of a message queue reader for the specified queue name.
        /// The reader can be used to receive messages from the queue.
        /// </summary>
        /// <param name="queueName">The name of the message queue to create.</param>
        /// <returns>A new instance of a message queue reader.</returns>
        IMessageQueueReader CreateReader(MessageQueueName queueName);

        /// <summary>
        /// Creates a new instance of a message queue sorter for the specified queue name.
        /// The sorter can be used to move messages in the queue to the sub-queues.
        /// </summary>
        /// <param name="queueName">The name of the message queue to create.</param>
        /// <returns>A new instance of a message queue sorter.</returns>
        IMessageQueueSorter CreateSorter(MessageQueueName queueName);

        /// <summary>
        /// Creates a new instance of a message queue cursor for the specified message queue reader.
        /// The cursor can be used to navigate the messages in the queue.
        /// </summary>
        /// <param name="reader">The message queue reader to create a cursor for.</param>
        /// <returns>A new instance of a message queue cursor.</returns>
        IMessageQueueCursor CreateCursor(IMessageQueueReader reader);
    }
}