namespace Dodkin
{
    using Interop;

    /// <summary>
    /// Maintains a specific location in a queue when reading the queue's messages.
    /// </summary>
    public class MessageQueueCursor : IMessageQueueCursor
    {
        private readonly MessageQueueReader reader;
        private readonly QueueCursorHandle cursorHandle;

        public MessageQueueCursor(MessageQueueReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);
            this.reader = reader;
            this.cursorHandle = QueueCursorHandle.Create(this.reader.Handle);
        }

        public MessageQueueName Name => this.reader.Name;

        public void Dispose()
        {
            this.cursorHandle.Dispose();
        }

        public Message Peek(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return this.reader.Receive(this.cursorHandle, ReceiveAction.PeekCurrent, properties, timeout, transaction);
        }

        public Task<Message> PeekAsync(MessageProperty properties, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return this.reader.ReceiveAsync(this.cursorHandle, ReceiveAction.PeekCurrent, properties, timeout, cancellationToken);
        }

        public Message PeekNext(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return this.reader.Receive(this.cursorHandle, ReceiveAction.PeekNext, properties, timeout, transaction);
        }

        public Task<Message> PeekNextAsync(MessageProperty properties, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return this.reader.ReceiveAsync(this.cursorHandle, ReceiveAction.PeekNext, properties, timeout, cancellationToken);
        }

        public Message Read(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return this.reader.Receive(this.cursorHandle, ReceiveAction.Receive, properties, timeout, transaction);
        }

        public Task<Message> ReadAsync(MessageProperty properties, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return this.reader.ReceiveAsync(this.cursorHandle, ReceiveAction.Receive, properties, timeout, cancellationToken);
        }
    }
}