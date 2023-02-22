namespace Dodkin
{
    using Interop;

    /// <summary>
    /// Represents a message queue opened for peeking at or retrieving messages.
    /// </summary>
    public class MessageQueueReader : MessageQueue, IMessageQueueReader
    {
        private readonly QueueConnection cnn;

        public MessageQueueReader(MessageQueueName queueName, QueueAccessMode accessMode = QueueAccessMode.Receive, QueueShareMode shareMode = QueueShareMode.Shared)
        {
            this.cnn = new QueueConnection(queueName, accessMode, shareMode);
        }

        public override MessageQueueName Name => this.cnn.QueueName;

        internal sealed override QueueHandle Handle => this.cnn.ReadHandle;

        public override void Dispose()
        {
            this.cnn.Dispose();
        }

        public void Purge()
        {
            MessageQueueException.ThrowIfNotOK(MQ.PurgeQueue(this.Handle));
        }

        public Message Peek(MessageProperty propertyFlags = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return Receive(QueueCursorHandle.None, ReceiveAction.PeekCurrent, propertyFlags, timeout, transaction);
        }

        public Task<Message> PeekAsync(MessageProperty propertyFlags, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return ReceiveAsync(QueueCursorHandle.None, ReceiveAction.PeekCurrent, propertyFlags, timeout, cancellationToken);
        }

        public Message Read(MessageProperty propertyFlags = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return Receive(QueueCursorHandle.None, ReceiveAction.Receive, propertyFlags, timeout, transaction);
        }

        public Task<Message> ReadAsync(MessageProperty propertyFlags, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return ReceiveAsync(QueueCursorHandle.None, ReceiveAction.Receive, propertyFlags, timeout, cancellationToken);
        }

        public async Task<Message> ReadAsync(MessageId correlationId, MessageProperty propertyFlags, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            using var cursorHandle = QueueCursorHandle.Create(this.Handle);
            using var peekProperties = new MessageProperties(MessageProperty.CorrelationId);

            for (var msg = await ReceiveAsync(cursorHandle, ReceiveAction.PeekCurrent, peekProperties, timeout, cancellationToken);
                !msg.IsEmpty;
                msg = await ReceiveAsync(cursorHandle, ReceiveAction.PeekNext, peekProperties, timeout, cancellationToken))
            {
                if (msg.CorrelationId == correlationId)
                {
                    return await ReceiveAsync(cursorHandle, ReceiveAction.Receive,
                        new MessageProperties(propertyFlags), timeout, cancellationToken);
                }
            }

            return default;
        }

        internal Task<Message> ReceiveAsync(QueueCursorHandle cursorHandle, ReceiveAction action, MessageProperty propertyFlags,
            TimeSpan? timeout, CancellationToken cancellationToken)
        {
            return ReceiveAsync(cursorHandle, action, new MessageProperties(propertyFlags), timeout, cancellationToken);
        }

        private Task<Message> ReceiveAsync(QueueCursorHandle cursorHandle, ReceiveAction action,
            MessageProperties properties, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var ar = new QueueAsyncRequest(this.cnn, cursorHandle, properties, cancellationToken)
            {
                Action = action,
                Timeout = timeout,
            };

            return ar.BeginRead();
        }

        internal unsafe Message Receive(QueueCursorHandle cursor, ReceiveAction action, MessageProperty properties, TimeSpan? timeout, QueueTransaction? transaction)
        {
            using var packedProperties = new MessageProperties(properties).Pack();
            while (true)
            {
                var result = transaction.TryGetHandle(out var transactionHandle) ?
                    MQ.ReceiveMessage(this.Handle, MQ.GetTimeout(timeout), action, packedProperties, null, null, cursor, transactionHandle) :
                    MQ.ReceiveMessage(this.Handle, MQ.GetTimeout(timeout), action, packedProperties, null, null, cursor, transaction.InternalTransaction);

                if (MQ.IsBufferOverflow(result))
                {
                    packedProperties.Adjust(result);
                    continue;
                }
                else if (MQ.IsStaleHandle(result))
                {
                    this.cnn.Close();
                    continue;
                }
                else if (result == MQ.HR.ERROR_IO_TIMEOUT)
                {
                    return default;
                }
                else
                {
                    MessageQueueException.ThrowOnError(result);
                }

                return packedProperties.Unpack<MessageProperties>();
            }
        }
    }
}
