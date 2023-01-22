namespace Dodkin
{
    using Interop;

    /// <summary>
    /// Represents a message subqueue opened for moving messages.
    /// </summary>
    public sealed class MessageQueueSorter : MessageQueueReader, IMessageQueueSorter
    {
        private readonly QueueConnection moveCnn;

        public MessageQueueSorter(MessageQueueName queueName, QueueAccessMode mode = QueueAccessMode.Receive, QueueShareMode share = QueueShareMode.Shared)
            : base(queueName, mode, share)
        {
            this.moveCnn = new QueueConnection(queueName, QueueAccessMode.Move, share);
        }

        public void Move(long lookupId, QueueTransaction? transaction = null)
        {
            var result = transaction.TryGetHandle(out var txnHandle) ?
                MQ.MoveMessage(base.Handle, this.moveCnn.ReadHandle, lookupId, txnHandle) :
                MQ.MoveMessage(base.Handle, this.moveCnn.ReadHandle, lookupId, transaction.InternalTransaction!);

            MessageQueueException.ThrowOnError(result);
        }

        public override void Dispose()
        {
            this.moveCnn.Dispose();
            base.Dispose();
        }
    }
}
