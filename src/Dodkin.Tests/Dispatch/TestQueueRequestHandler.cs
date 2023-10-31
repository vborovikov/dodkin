namespace Dodkin.Tests.Dispatch;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dodkin;
using Dodkin.Dispatch;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

class TestQuery : Query<string>
{
}

class TestUnknownQuery : Query<string>
{
}

class TestCommand : Command
{
}

class TestUnknownCommand : Command
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
