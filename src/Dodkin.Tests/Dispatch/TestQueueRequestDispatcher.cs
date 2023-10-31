using Dodkin.Dispatch;
using Microsoft.Extensions.Logging;

namespace Dodkin.Tests.Dispatch;

internal class TestQueueRequestDispatcher : QueueRequestDispatcher
{
    public TestQueueRequestDispatcher(MessageQueueName requestQueueName, MessageEndpoint endpoint, ILogger<TestQueueRequestDispatcher> logger) 
        : base(requestQueueName, endpoint, logger)
    {
    }
}