namespace Dodkin
{
    public interface IMessageQueueSorter : IMessageQueueReader
    {
        void Move(long lookupId, QueueTransaction? transaction = null);
    }
}