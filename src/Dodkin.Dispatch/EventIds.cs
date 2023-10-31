namespace Dodkin.Dispatch;

using Microsoft.Extensions.Logging;

static class EventIds
{
    // Messaging

    public static readonly EventId MessageAckNack = new(1, "MessageAckNack");
    public static readonly EventId ProcessingFailed = new(2, "ProcessingFailed");

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

    public static readonly EventId RequestReceived = new(30, "RequestReceived");
    public static readonly EventId RequestRejected = new(31, "RequestRejected");
    public static readonly EventId CommandRecognized = new(32, "CommandRecognized");
    public static readonly EventId QueryRecognized = new(33, "QueryRecognized");
    public static readonly EventId RequestFailed = new(34, "RequestFailed");

    public static readonly EventId CommandExecuted = new(40, "CommandExecuted");
    public static readonly EventId CommandNotImplemented = new(41, "CommandNotImplemented");
    public static readonly EventId CommandExecutionFailed = new(42, "CommandExecutionFailed");
    public static readonly EventId QueryExecuted = new(43, "QueryExecuted");
    public static readonly EventId QueryResultSent = new(44, "QueryResultSent");
    public static readonly EventId QueryNotImplemented = new(45, "QueryNotImplemented");
    public static readonly EventId QueryExecutionFailed = new(46, "QueryExecutionFailed");
}