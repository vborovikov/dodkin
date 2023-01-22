namespace Dodkin
{
    /// <summary>
    /// Represents a message subqueue opened for moving messages.
    /// </summary>
    public interface IMessageQueueSorter : IMessageQueueReader
    {
        void Move(long lookupId, QueueTransaction? transaction = null);
    }
}