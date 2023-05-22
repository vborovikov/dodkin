namespace Dodkin
{
    using Interop;

    /// <summary>
    /// Represents a message queue opened for sending messages.
    /// </summary>
    public sealed class MessageQueueWriter : MessageQueue, IMessageQueueWriter
    {
        private readonly QueueConnection cnn;

        public MessageQueueWriter(MessageQueueName queueName, QueueAccessMode accessMode = QueueAccessMode.Send, QueueShareMode shareMode = QueueShareMode.Shared)
        {
            this.cnn = new QueueConnection(queueName, accessMode, shareMode);
        }

        public override MessageQueueName Name => this.cnn.QueueName;

        internal override QueueHandle Handle => this.cnn.WriteHandle;

        public override void Dispose()
        {
            this.cnn.Dispose();
        }

        /// <summary>
        /// Asks MSMQ to attempt to deliver a message.
        /// To ensure the message reached the queue you need to check acknowledgement messages sent to the <see cref="Message.AdministrationQueue"/>
        /// </summary>
        /// <param name="message">The message to try to send</param>
        /// <param name="transaction">can be NULL for no transaction, a <see cref="QueueTransaction"/>, <see cref="QueueTransaction.SingleMessage"/>, or <see cref="QueueTransaction.DTC"/>.</param>
        public void Write(Message message, QueueTransaction? transaction = null)
        {
            ArgumentNullException.ThrowIfNull(message);

            using var props = message.Properties.Pack();
            var result = transaction.TryGetHandle(out var txnHandle) ?
                MQ.SendMessage(this.Handle, props, txnHandle) :
                MQ.SendMessage(this.Handle, props, transaction.InternalTransaction!);

            MessageQueueException.ThrowOnError(result, props);
        }

        public Task WriteAsync(Message message, QueueTransaction? transaction = null, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Write(message, transaction), cancellationToken);
        }
    }
}
