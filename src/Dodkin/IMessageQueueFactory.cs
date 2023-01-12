namespace Dodkin
{
    public interface IMessageQueueFactory
    {
        IMessageQueueWriter CreateWriter(MessageQueueName queueName);
        IMessageQueueReader CreateReader(MessageQueueName queueName);
        IMessageQueueSorter CreateSorter(MessageQueueName queueName);
        IMessageQueueCursor CreateCursor(IMessageQueueReader reader);
    }
}