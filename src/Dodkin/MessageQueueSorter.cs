namespace Dodkin
{
    using System;
    using Interop;

    /// <summary>
    /// Represents a message queue and/or subqueue opened for sorting messages.
    /// </summary>
    public sealed class MessageQueueSorter : MessageQueueReader, IMessageQueueSorter
    {
        private readonly QueueConnection moveCnn;

        public MessageQueueSorter(MessageQueueName queueName, QueueAccessMode mode = QueueAccessMode.Receive, QueueShareMode share = QueueShareMode.Shared)
            : base(EnsureQueueName(queueName), mode, share)
        {
            this.moveCnn = new QueueConnection(queueName, QueueAccessMode.Move, share);
        }

        public Message Read(MessageLookupId lookupId, MessageProperty properties = MessageProperty.All, QueueTransaction? transaction = null)
        {
            return base.Receive(lookupId, LookupAction.ReceiveCurrent, properties, transaction);
        }

        public void Move(MessageLookupId lookupId, QueueTransaction? transaction = null)
        {
            var result = transaction.TryGetHandle(out var txnHandle) ?
                MQ.MoveMessage(base.Handle, this.moveCnn.ReadHandle, lookupId.Value, txnHandle) :
                MQ.MoveMessage(base.Handle, this.moveCnn.ReadHandle, lookupId.Value, transaction.InternalTransaction!);

            MessageQueueException.ThrowOnError(result);
        }

        public void Reject(MessageLookupId lookupId)
        {
            MessageQueueException.ThrowOnError(MQ.MarkMessageRejected(base.Handle, lookupId.Value));
        }

        public override void Dispose()
        {
            this.moveCnn.Dispose();
            base.Dispose();
        }

        private static MessageQueueName EnsureQueueName(MessageQueueName queueName)
        {
            var formatName = queueName.FormatName;
            var separatorPos = formatName.LastIndexOf(';');
            if (separatorPos > 0)
            {
                return MessageQueueName.Parse(formatName.AsSpan(0, separatorPos));
            }

            return queueName;
        }
    }
}
