namespace Dodkin.Tests.Dispatch;

using Dodkin;
using Dodkin.Dispatch;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

record TestQuery : Query<string>
{
}

record TestUnknownQuery : Query<string>
{
}

record TestCommand : Command
{
}

record TestUnknownCommand : Command
{
}

internal class TestQueueRequestHandler : QueueRequestHandler,
    IQueryHandler<TestQuery, string>
{
    public TestQueueRequestHandler(MessageEndpoint endpoint, ILogger<TestQueueRequestHandler> logger)
        : base(endpoint, logger)
    {
    }

    public string Run(TestQuery query)
    {
        return "Hello";
    }
}
