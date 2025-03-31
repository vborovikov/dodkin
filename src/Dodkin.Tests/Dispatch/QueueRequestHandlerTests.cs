namespace Dodkin.Tests.Dispatch;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Message = Dodkin.Message;

#nullable disable

[TestClass]
public class QueueRequestHandlerTests
{
    private static ILoggerFactory loggerFactory;
    private static MessageEndpoint handlerME;
    private static MessageEndpoint dispatcherME;
    private static CancellationTokenSource cancelSource;
    private static TestQueueRequestHandler requestHandler;
    private static TestQueueRequestDispatcher requestDispatcher;
    private static Task processingTask;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(); // Add the console logger
            builder.AddDebug(); // Add the debug logger
        });

        handlerME = MessageEndpoint.FromName("test-handler");
        dispatcherME = MessageEndpoint.FromName("test-dispatch");

        cancelSource = new CancellationTokenSource();
        requestHandler = new(handlerME, loggerFactory.CreateLogger<TestQueueRequestHandler>());
        requestDispatcher = new(handlerME.ApplicationQueue, dispatcherME, loggerFactory.CreateLogger<TestQueueRequestDispatcher>());

        processingTask = requestHandler.ProcessAsync(cancelSource.Token);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        processingTask = null;
        cancelSource.Cancel();

        dispatcherME.DeleteIfExists();
        handlerME.DeleteIfExists();

        loggerFactory.Dispose();
    }

    [TestMethod]
    public async Task ProcessAsync_Query_ResultReceived()
    {
        var result = await requestDispatcher.RunAsync(new TestQuery(3));
        Assert.IsNotNull(result);
        Assert.AreEqual("Hello", result);
    }

    [TestMethod]
    public async Task ProcessAsync_UnknownQuery_ExceptionThrown()
    {
        await Assert.ThrowsExceptionAsync<TimeoutException>(() => requestDispatcher.RunAsync(new TestUnknownQuery()));
    }

    [TestMethod]
    public async Task ProcessAsync_Shutdown_ProcessingStops()
    {
        var requestHandler = new TestQueueRequestHandler(handlerME, loggerFactory.CreateLogger<TestQueueRequestHandler>());
        var cancelSource = new CancellationTokenSource();
        var processingTask = requestHandler.ProcessAsync(cancelSource.Token);
        await Task.Delay(100);
        cancelSource.Cancel();
        await Task.Delay(100);
        Assert.IsTrue(processingTask.IsCompleted);
    }

    [TestMethod]
    public async Task DispatchRequestsAsync_Command_DispatchedToCommandSubqueue()
    {
        var command = new TestCommand(Random.Shared.Next());
        await requestDispatcher.ExecuteAsync(command);

        await Task.Delay(500); // Allow time for dispatching

        Assert.AreEqual(command.Parameter, (requestHandler.ExecutedCommand as TestCommand)?.Parameter);
    }

    [TestMethod]
    public async Task DispatchRequestsAsync_Query_DispatchedToQuerySubqueue()
    {
        var query = new TestQuery(Random.Shared.Next());
        var result = await requestDispatcher.RunAsync(query);

        await Task.Delay(500); // Allow time for dispatching

        Assert.AreEqual(query.Parameter, (requestHandler.RunQuery as TestQuery).Parameter);
    }

    [TestMethod]
    public async Task DispatchRequestsAsync_UnknownRequest_Rejected()
    {
        using var message = new Message("Unknown"u8.ToArray())
        {
            Acknowledgment = MessageAcknowledgment.FullReceive,
            AdministrationQueue = dispatcherME.AdministrationQueue,
            ResponseQueue = dispatcherME.ApplicationQueue,
        };
        using var appQ = MessageQueueFactory.Instance.CreateWriter(handlerME.ApplicationQueue);
        appQ.Write(message, QueueTransaction.SingleMessage);

        await Task.Delay(500); // Allow time for dispatching

        // Assert
        using var adminQ = MessageQueueFactory.Instance.CreateReader(dispatcherME.AdministrationQueue);
        using var ack = await adminQ.ReadAsync(message.Id, MessageProperty.Class, TimeSpan.FromSeconds(1));
        Assert.AreEqual(MessageClass.NackReceiveRejected, ack.Class);
    }

    //[TestMethod]
    public async Task DispatchRequestsAsync_UnknownRequest_CanDispatchRequestTrue_Rejected()
    {
        using var message = new Message("Unknown"u8.ToArray())
        {
            Acknowledgment = MessageAcknowledgment.FullReceive,
            AdministrationQueue = dispatcherME.AdministrationQueue,
            ResponseQueue = dispatcherME.ApplicationQueue,
        };

        requestHandler.CanDispatchRequestResult = true;
        requestHandler.TryDispatchRequestResult = true;
        using var appQ = MessageQueueFactory.Instance.CreateWriter(handlerME.ApplicationQueue);
        appQ.Write(message, QueueTransaction.SingleMessage);

        await Task.Delay(500); // Allow time for dispatching

        // Assert
        using var adminQ = MessageQueueFactory.Instance.CreateReader(dispatcherME.AdministrationQueue);
        using var ack = await adminQ.ReadAsync(message.Id, MessageProperty.Class, TimeSpan.FromSeconds(1));
        requestHandler.CanDispatchRequestResult = false;
        requestHandler.TryDispatchRequestResult = false;

        Assert.AreEqual(MessageClass.NackReceiveRejected, ack.Class);
    }

    [TestMethod]
    public async Task DispatchRequestsAsync_CanDispatchRequestTrueTryDispatchRequestFalse_DeferredToRequestSubqueue()
    {
        using var message = new Message("Unknown"u8.ToArray())
        {
            Acknowledgment = MessageAcknowledgment.FullReceive,
            AdministrationQueue = dispatcherME.AdministrationQueue,
            ResponseQueue = dispatcherME.ApplicationQueue,
        };

        requestHandler.CanDispatchRequestResult = true;
        requestHandler.TryDispatchRequestResult = false;
        using var appQ = MessageQueueFactory.Instance.CreateWriter(handlerME.ApplicationQueue);
        appQ.Write(message, QueueTransaction.SingleMessage);

        await Task.Delay(500); // Allow time for dispatching

        // Assert
        using var reqQ = MessageQueueFactory.Instance.CreateReader(handlerME.ApplicationQueue.GetSubqueueName("requests"));
        using var dispatchedMessage = reqQ.Read(MessageProperty.All, TimeSpan.FromSeconds(1));
        requestHandler.CanDispatchRequestResult = false;
        requestHandler.TryDispatchRequestResult = false;

        Assert.AreEqual(message.Id, dispatchedMessage.Id);
    }

    [TestMethod]
    public async Task DispatchRequestsAsync_Exception_MessageFailed()
    {
        // Arrange
        requestHandler.CanDispatchRequestResult = true;
        requestHandler.ThrowException = true;
        await Assert.ThrowsExceptionAsync<TimeoutException>(async () => await requestDispatcher.RunAsync(new TestQuery(9)));

        // Act
        await Task.Delay(500); // Allow time for dispatching

        // Assert
        using var dlQ = MessageQueueFactory.Instance.CreateReader(handlerME.DeadLetterQueue);
        using var deadLetter = dlQ.Read(MessageProperty.All, TimeSpan.FromSeconds(1));
        requestHandler.ThrowException = false;
        requestHandler.CanDispatchRequestResult = false;

        Assert.AreEqual("TestQuery", deadLetter.Label);
    }
}
