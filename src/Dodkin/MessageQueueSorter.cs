namespace Dodkin;

using Interop;

/// <summary>
/// Represents a message queue opened for sorting messages.
/// </summary>
public sealed class MessageQueueSorter : MessageQueueReader, IMessageQueueSorter
{
    public MessageQueueSorter(MessageQueueName queueName, QueueAccessMode mode = QueueAccessMode.Receive, QueueShareMode share = QueueShareMode.Shared)
        : base(queueName, mode, share)
    {
    }

    public Message Read(MessageLookupId lookupId, MessageProperty properties = MessageProperty.All, QueueTransaction? transaction = null)
    {
        return base.Receive(lookupId, LookupAction.ReceiveCurrent, properties, transaction);
    }

    public void Move(MessageLookupId lookupId, Subqueue subqueue, QueueTransaction? transaction = null)
    {
        var result = transaction.TryGetHandle(out var txnHandle) ?
            MQ.MoveMessage(base.Handle, subqueue.Handle, lookupId.Value, txnHandle) :
            MQ.MoveMessage(base.Handle, subqueue.Handle, lookupId.Value, transaction.InternalTransaction!);

        MessageQueueException.ThrowOnError(result);
    }

    public void Reject(MessageLookupId lookupId, QueueTransaction transaction)
    {
        using var msg = Read(lookupId, MessageProperty.LookupId, transaction);
        MessageQueueException.ThrowOnError(MQ.MarkMessageRejected(base.Handle, msg.LookupId.Value));
    }
}
