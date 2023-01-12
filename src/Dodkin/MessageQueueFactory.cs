namespace Dodkin
{
    public sealed class MessageQueueFactory : IMessageQueueFactory
    {
        public IMessageQueueWriter CreateWriter(MessageQueueName queueName)
        {
            return new MessageQueueWriter(queueName);
        }

        public IMessageQueueReader CreateReader(MessageQueueName queueName)
        {
            return new MessageQueueReader(queueName);
        }

        public IMessageQueueSorter CreateSorter(MessageQueueName queueName)
        {
            return new MessageQueueSorter(queueName);
        }

        public IMessageQueueCursor CreateCursor(IMessageQueueReader reader)
        {
            return new MessageQueueCursor((MessageQueueReader)reader);
        }
    }
}
