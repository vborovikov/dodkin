﻿namespace Dodkin
{
    using Interop;

    public class MessageQueueReader : MessageQueue, IMessageQueueReader
    {
        private readonly QueueConnection cnn;

        public MessageQueueReader(MessageQueueName queueName, QueueAccessMode accessMode = QueueAccessMode.Receive, QueueShareMode shareMode = QueueShareMode.Shared)
        {
            this.cnn = new QueueConnection(queueName, accessMode, shareMode);
        }

        public override string FormatName => this.cnn.FormatName;

        internal sealed override QueueHandle Handle => this.cnn.ReadHandle;

        public override void Dispose()
        {
            this.cnn.Dispose();
        }

        public void Purge()
        {
            MessageQueueException.ThrowIfNotOK(MQ.PurgeQueue(this.Handle));
        }

        public Message Peek(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return Receive(QueueCursorHandle.None, ReadAction.PeekCurrent, properties, timeout, transaction);
        }

        public Task<Message> PeekAsync(MessageProperty properties, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return ReceiveAsync(QueueCursorHandle.None, ReadAction.PeekCurrent, properties, timeout, cancellationToken);
        }

        public Message Read(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = null, QueueTransaction? transaction = null)
        {
            return Receive(QueueCursorHandle.None, ReadAction.Receive, properties, timeout, transaction);
        }

        public Task<Message> ReadAsync(MessageProperty properties, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return ReceiveAsync(QueueCursorHandle.None, ReadAction.Receive, properties, timeout, cancellationToken);
        }

        public async Task<Message> ReadAsync(MessageId correlationId, MessageProperty properties, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            using var cursorHandle = QueueCursorHandle.Create(this.Handle);
            using var peekProperties = new MessageProperties(MessageProperty.CorrelationId);
            using var packedProperties = peekProperties.Pack();

            for (var msg = await ReceiveAsync(cursorHandle, ReadAction.PeekCurrent, packedProperties, false, timeout, cancellationToken);
                !msg.IsEmpty;
                msg = await ReceiveAsync(cursorHandle, ReadAction.PeekNext, packedProperties, false, timeout, cancellationToken))
            {
                if (msg.CorrelationId == correlationId)
                {
                    return await ReceiveAsync(cursorHandle, ReadAction.Receive, 
                        new MessageProperties(properties).Pack(), true, timeout, cancellationToken);
                }
            }

            return default;
        }

        internal Task<Message> ReceiveAsync(QueueCursorHandle cursorHandle, ReadAction action, MessageProperty properties,
            TimeSpan? timeout, CancellationToken cancellationToken)
        {
            return ReceiveAsync(cursorHandle, action, new MessageProperties(properties).Pack(), true, timeout, cancellationToken);
        }

        private Task<Message> ReceiveAsync(QueueCursorHandle cursorHandle, ReadAction action, 
            MessageProperties.Package properties, bool disposeProperties,
            TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var ar = new AsyncQueueRequest(this.cnn, cursorHandle, properties, cancellationToken)
            {
                Action = action,
                Timeout = timeout,
                OwnsProperties = disposeProperties,
            };

            return ar.BeginRead();
        }

        internal unsafe Message Receive(QueueCursorHandle cursor, ReadAction action, MessageProperty properties, TimeSpan? timeout, QueueTransaction? transaction)
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
