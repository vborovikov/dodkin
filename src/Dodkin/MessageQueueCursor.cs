namespace Dodkin
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Interop;

    /// <summary>
    /// Maintains a specific location in a queue when reading the queue's messages.
    /// </summary>
    public sealed class MessageQueueCursor : IMessageQueueCursor
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

        public bool IsTransactional => this.reader.IsTransactional;

        public void Dispose()
        {
            this.cursorHandle.Dispose();
        }

        public Message Peek(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return this.reader.Receive(this.cursorHandle, ReceiveAction.PeekCurrent, properties, timeout, transaction);
        }

        public IAsyncEnumerable<Message> PeekAllAsync(MessageProperty propertyFlags, CancellationToken cancellationToken = default)
        {
            return InternalPeekAllAsync(propertyFlags, cancellationToken);
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

        public IAsyncEnumerable<Message> ReadAllAsync(MessageProperty propertyFlags, CancellationToken cancellationToken = default)
        {
            return this.reader.InternalReceiveAllAsync(this.cursorHandle, propertyFlags, cancellationToken);
        }

        public Task<Message> ReadAsync(MessageProperty properties, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return this.reader.ReceiveAsync(this.cursorHandle, ReceiveAction.Receive, properties, timeout, cancellationToken);
        }

        private async IAsyncEnumerable<Message> InternalPeekAllAsync(MessageProperty propertyFlags,
            CancellationToken cancellationToken, [EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, enumeratorCancellationToken);

            for (var peekAction = ReceiveAction.PeekCurrent; ; peekAction = ReceiveAction.PeekNext)
            {
                // creating message properties in a loop to enable concurrent enumeration

                var messageProperties = new MessageProperties(propertyFlags);
                var message = await this.reader.ReceiveAsync(this.cursorHandle, peekAction, messageProperties, null, cts.Token).ConfigureAwait(false);
                yield return message;
            }
        }
    }
}