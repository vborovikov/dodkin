namespace Dodkin.Tests.Dispatch;

using System.Threading;
using Dodkin;
using Dodkin.Dispatch;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

record TestQuery(int Parameter) : Query<string> { }

record TestUnknownQuery : Query<string> { }

record TestCommand(int Parameter) : Command { }

record TestUnknownCommand : Command { }

record TestRequest : IRequest
{
    public CancellationToken CancellationToken { get; set; }
}

record TestUnknownRequest : IRequest
{
    public CancellationToken CancellationToken { get; set; }
}

internal class TestQueueRequestHandler : QueueRequestHandler,
    IQueryHandler<TestQuery, string>,
    ICommandHandler<TestCommand>
{
    public TestQueueRequestHandler(MessageEndpoint endpoint, ILogger<TestQueueRequestHandler> logger)
        : base(endpoint, logger)
    {
    }

    public bool CanDispatchRequestResult { get; set; }
    public bool TryDispatchRequestResult { get; set; }
    public bool ThrowException { get; set; }
    public ICommand? ExecutedCommand { get; private set; }
    public IQuery? RunQuery { get; private set; }

    protected override bool CanDispatchRequest(in Message message)
    {
        return this.CanDispatchRequestResult;
    }

    protected override bool TryDispatchRequest(in Message message)
    {
        if (this.ThrowException)
        {
            throw new Exception("Test Exception");
        }
        return this.TryDispatchRequestResult;
    }

    public string Run(TestQuery query)
    {
        this.RunQuery = query;
        return "Hello";
    }

    public void Execute(TestCommand command)
    {
        this.ExecutedCommand = command;
    }
}
