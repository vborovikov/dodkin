namespace Dodkin
{
    /// <summary>Can one or many processes read from the queue?</summary>
    public enum QueueShareMode
    {
        /// <summary>Any process can read messages from the queue</summary>
        Shared = 0,

        /// <summary>Only this <see cref="MessageQueueReader"/> can read messages from the queue, no other process can</summary>
        ExclusiveReceive = 1,
    }
}