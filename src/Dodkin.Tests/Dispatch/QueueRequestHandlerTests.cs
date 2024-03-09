namespace Dodkin.Tests.Dispatch;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
        var result = await requestDispatcher.RunAsync(new TestQuery());
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
}
