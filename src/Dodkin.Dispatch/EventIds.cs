namespace Dodkin.Dispatch;

using Microsoft.Extensions.Logging;

static class EventIds
{
    // Messaging

    public static readonly EventId ProcessingStarted = new(1, "ProcessingStarted");
    public static readonly EventId ProcessingStopped = new(2, "ProcessingStopped");
    public static readonly EventId DispatchingFailed = new(3, "DispatchingFailed");
    public static readonly EventId MessageAckNack = new(4, "MessageAckNack");
    public static readonly EventId EndpointFailed = new(5, "EndpointFailed");
    public static readonly EventId DispatchingStarted = new(6, "DispatchingStarted");
    public static readonly EventId DispatchingStopped = new(7, "DispatchingStopped");

    // Dispatcher events

    public static readonly EventId CommandSent = new(10, "CommandSent");
    public static readonly EventId CommandConfirmed = new(11, "CommandConfirmed");
    public static readonly EventId CommandFailed = new(12, "CommandFailed");
    public static readonly EventId CommandTimedOut = new(13, "CommandTimedOut");

    public static readonly EventId QuerySent = new(20, "QuerySent");
    public static readonly EventId QueryConfirmed = new(21, "QueryConfirmed");
    public static readonly EventId QueryFailed = new(22, "QueryFailed");
    public static readonly EventId QueryTimedOut = new(23, "QueryTimedOut");
    public static readonly EventId ResultReceived = new(24, "ResultReceived");

    // Handler events

    public static readonly EventId MessageReceived = new(30, "MessageReceived");
    public static readonly EventId MessageRejected = new(31, "MessageRejected");
    public static readonly EventId CommandDispatched = new(32, "CommandDispatched");
    public static readonly EventId QueryDispatched = new(33, "QueryDispatched");
    public static readonly EventId MessageFailed = new(34, "MessageFailed");
    public static readonly EventId RequestDispatched = new(35, "RequestDispatched");
    public static readonly EventId RequestDeferred = new(36, "RequestDeferred");

    public static readonly EventId CommandExecuted = new(40, "CommandExecuted");
    public static readonly EventId CommandNotImplemented = new(41, "CommandNotImplemented");
    public static readonly EventId CommandExecutionFailed = new(42, "CommandExecutionFailed");
    public static readonly EventId QueryExecuted = new(43, "QueryExecuted");
    public static readonly EventId QueryResultSent = new(44, "QueryResultSent");
    public static readonly EventId QueryNotImplemented = new(45, "QueryNotImplemented");
    public static readonly EventId QueryExecutionFailed = new(46, "QueryExecutionFailed");
    public static readonly EventId CommandExecutionCancelled = new(47, "CommandExecutionCancelled");
    public static readonly EventId QueryExecutionCancelled = new(48, "QueryExecutionCancelled");
}