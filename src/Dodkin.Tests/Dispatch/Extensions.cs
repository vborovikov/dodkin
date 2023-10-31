namespace Dodkin.Tests.Dispatch;

static class Extensions
{
    /// <summary>
    /// Deletes the message queues if they exist.
    /// </summary>
    public static void DeleteIfExists(this MessageEndpoint endpoint)
    {
        if (MessageQueue.Exists(endpoint.DeadLetterQueue))
        {
            MessageQueue.Delete(endpoint.DeadLetterQueue);
        }
        if (MessageQueue.Exists(endpoint.AdministrationQueue))
        {
            MessageQueue.Delete(endpoint.AdministrationQueue);
        }
        if (MessageQueue.Exists(endpoint.ApplicationQueue))
        {
            MessageQueue.Delete(endpoint.ApplicationQueue);
        }
    }
}
