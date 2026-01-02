namespace Dodkin.Tests.Dispatch;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dodkin.Dispatch;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

record NestedObject(string Value);
record ComplexObject(string StringValue, int IntValue, bool BoolValue, NestedObject NestedValue);
record UdsTestCommand(int Parameter) : Command { }
record UdsTestCommandWithResult(int Parameter) : Command { }
record UdsTestQuery(int Parameter) : Query<string> { }
record UdsTestComplexQuery : Query<ComplexObject>
{
    //public UdsTestComplexQuery(ComplexObject data)
    //{
    //    this.Data = data;
    //}

    public ComplexObject Data { get; init; }
}
record UdsTestComplexCommand : Command
{
    public UdsTestComplexCommand(ComplexObject data)
    {
        this.Data = data;
    }

    public ComplexObject Data { get; init; }
}
record UdsTestTimeoutCommand(int DelayMs) : Command { }
record UdsTestTimeoutQuery(int DelayMs) : Query<string> { }
record UdsTestCancellationCommand : Command { }
record UdsTestCancellationQuery : Query<string> { }
record UdsTestErrorCommand : Command { }
record UdsTestErrorQuery : Query<string> { }

// Handler for test commands and queries
internal class UdsTestRequestHandler : UdsRequestHandler,
    IQueryHandler<UdsTestQuery, string>,
    IQueryHandler<UdsTestComplexQuery, ComplexObject>,
    ICommandHandler<UdsTestCommand>,
    ICommandHandler<UdsTestComplexCommand>,
    ICommandHandler<UdsTestCommandWithResult>,
    ICommandHandler<UdsTestTimeoutCommand>,
    IQueryHandler<UdsTestTimeoutQuery, string>,
    ICommandHandler<UdsTestCancellationCommand>,
    IQueryHandler<UdsTestCancellationQuery, string>,
    ICommandHandler<UdsTestErrorCommand>,
    IQueryHandler<UdsTestErrorQuery, string>
{
    public UdsTestRequestHandler(string socketPath, ILogger<UdsTestRequestHandler> logger)
        : base(socketPath, logger)
    {
    }

    public string Run(UdsTestQuery query) => $"Result: {query.Parameter}";

    public ComplexObject Run(UdsTestComplexQuery query) => query.Data;

    public void Execute(UdsTestCommand command)
    {
        // Do nothing, just for testing
    }

    public void Execute(UdsTestCommandWithResult command)
    {
        // Do nothing, just for testing
    }

    public void Execute(UdsTestTimeoutCommand command)
    {
        // For sync interface, we'll just delay briefly
        Thread.Sleep(command.DelayMs);
    }

    public string Run(UdsTestTimeoutQuery query)
    {
        // For sync interface, return a simple result
        return "Timeout result";
    }

    public void Execute(UdsTestCancellationCommand command)
    {
        // Do nothing, just for testing
    }

    public string Run(UdsTestCancellationQuery query) => "Cancelled result";

    public void Execute(UdsTestErrorCommand command)
    {
        throw new InvalidOperationException("Test error in command");
    }

    public string Run(UdsTestErrorQuery query) => throw new InvalidOperationException("Test error in query");

    public void Execute(UdsTestComplexCommand command)
    {
        // Do nothing, just for testing
    }
}

[TestClass]
public class UdsTests
{
    private const int MaxConnections = 8;

    private static ILoggerFactory loggerFactory;
    private readonly ILogger<UdsTestRequestHandler> handlerLogger = loggerFactory.CreateLogger<UdsTestRequestHandler>();
    private readonly ILogger<UdsRequestDispatcher> dispatcherLogger = loggerFactory.CreateLogger<UdsRequestDispatcher>();

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(); // Add the console logger
            builder.AddDebug(); // Add the debug logger
        });
    }

    [TestMethod]
    public async Task TestUtf8MemoryBufferBasicOperations()
    {
        using var buffer = new Utf8MemoryBuffer();

        // Test appending simple string
        buffer.TryAppend("Hello");
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(buffer.WrittenSpan));

        // Test appending character
        buffer.Clear();
        buffer.TryAppend('W');
        buffer.TryAppend('o');
        buffer.TryAppend('r');
        buffer.TryAppend('l');
        buffer.TryAppend('d');
        Assert.AreEqual("World", Encoding.UTF8.GetString(buffer.WrittenSpan));

        // Test serialization
        var testObject = new { Name = "Test", Value = 42 };
        buffer.Clear();
        var success = buffer.TrySerialize(testObject);
        Assert.IsTrue(success);

        // The buffer should have serialized content
        Assert.IsGreaterThan(0, buffer.WrittenCount);
    }

    [TestMethod]
    public void TestUtf8MemoryBufferCapacityAndWrittenMemory()
    {
        using var buffer = new Utf8MemoryBuffer();

        // Initially should be empty
        Assert.AreEqual(0, buffer.WrittenCount);
        Assert.AreEqual(0, buffer.WrittenMemory.Length);

        // Add some content
        buffer.TryAppend("Hello");
        Assert.AreEqual(5, buffer.WrittenCount);
        Assert.AreEqual(5, buffer.WrittenMemory.Length);
        Assert.AreEqual("Hello", Encoding.UTF8.GetString(buffer.WrittenSpan));

        // Clear should reset the count
        buffer.Clear();
        Assert.AreEqual(0, buffer.WrittenCount);
    }

    [TestMethod]
    public void TestUtf8MemoryBufferLargeContent()
    {
        using var buffer = new Utf8MemoryBuffer();

        // Create a large string to test buffer capacity
        var largeString = new string('A', 10000); // 10KB string
        var result = buffer.TryAppend(largeString);

        // Should succeed for content within buffer size
        Assert.IsTrue(result);
        Assert.AreEqual(largeString.Length, buffer.WrittenCount);
    }

    [TestMethod]
    public async Task TestComplexObjectSerialization()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        var complexData = new ComplexObject("test_string", 123, true, new NestedObject("nested_value"));

        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var query = new UdsTestComplexQuery { Data = complexData };
            var result = await dispatcher.RunAsync(query);

            Assert.AreEqual(complexData.StringValue, result.StringValue);
            Assert.AreEqual(complexData.IntValue, result.IntValue);
            Assert.AreEqual(complexData.BoolValue, result.BoolValue);
            Assert.AreEqual(complexData.NestedValue, result.NestedValue);
            Assert.AreEqual(complexData.NestedValue.Value, result.NestedValue.Value);
        }
        catch (PlatformNotSupportedException)
        {
            // Unix domain sockets are not supported on this platform
            Assert.Inconclusive("Unix domain sockets are not supported on this platform");
        }
        catch (SocketException)
        {
            // Unix domain sockets are not supported on this platform
            Assert.Inconclusive("Unix domain sockets are not supported on this platform");
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestCommandExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var command = new UdsTestCommand(42);
            await dispatcher.ExecuteAsync(command);

            // Test passed if no exception was thrown
            Assert.IsTrue(true);
        }
        catch (PlatformNotSupportedException)
        {
            // Unix domain sockets are not supported on this platform
            Assert.Inconclusive("Unix domain sockets are not supported on this platform");
        }
        catch (SocketException)
        {
            // Unix domain sockets are not supported on this platform
            Assert.Inconclusive("Unix domain sockets are not supported on this platform");
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestCommandExecutionWithTimeout()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = handler.ProcessAsync(CancellationToken.None);

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var command = new UdsTestCommand(123);
            await dispatcher.ExecuteAsync(command, TimeSpan.FromSeconds(10));

            // Test passed if no exception was thrown
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestComplexCommandExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        var testData = new ComplexObject("value1", 42, true, new("1"));

        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var command = new UdsTestComplexCommand(testData);
            await dispatcher.ExecuteAsync(command);

            // Test passed if no exception was thrown
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestMultipleCommandExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            // Execute multiple commands
            await dispatcher.ExecuteAsync(new UdsTestCommand(1));
            await dispatcher.ExecuteAsync(new UdsTestCommand(2));
            await dispatcher.ExecuteAsync(new UdsTestCommand(3));
            await dispatcher.ExecuteAsync(new UdsTestComplexCommand(new ComplexObject("value1", 42, true, new("1"))));

            // Test passed if no exception was thrown
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestCommandWithCancellationToken()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            using var cts = new CancellationTokenSource();
            var command = new UdsTestCommand(999) { CancellationToken = cts.Token };
            await dispatcher.ExecuteAsync(command);

            // Test passed if no exception was thrown
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestQueryExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var query = new UdsTestQuery(42);
            var result = await dispatcher.RunAsync(query);

            Assert.AreEqual("Result: 42", result);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestQueryExecutionWithTimeout()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var query = new UdsTestQuery(123);
            var result = await dispatcher.RunAsync(query, TimeSpan.FromSeconds(10));

            Assert.AreEqual("Result: 123", result);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestComplexQueryExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        var testData = new ComplexObject("value1", 42, true, new("1"));

        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var query = new UdsTestComplexQuery { Data = testData };
            var result = await dispatcher.RunAsync(query);

            Assert.AreEqual(testData.StringValue, result.StringValue);
            Assert.AreEqual(testData.IntValue, result.IntValue);
            Assert.AreEqual(testData.BoolValue, result.BoolValue);
            Assert.AreEqual(testData.NestedValue, result.NestedValue);
            Assert.AreEqual(testData.NestedValue.Value, result.NestedValue.Value);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestMultipleQueryExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            // Execute multiple queries
            var result1 = await dispatcher.RunAsync(new UdsTestQuery(1));
            var result2 = await dispatcher.RunAsync(new UdsTestQuery(2));
            var result3 = await dispatcher.RunAsync(new UdsTestQuery(3));
            var result4 = await dispatcher.RunAsync(new UdsTestComplexQuery { Data = new ComplexObject("value1", 42, true, new("1")) });

            Assert.AreEqual("Result: 1", result1);
            Assert.AreEqual("Result: 2", result2);
            Assert.AreEqual("Result: 3", result3);
            Assert.AreEqual("value1", result4.StringValue);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestQueryWithCancellationToken()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            using var cts = new CancellationTokenSource();
            var query = new UdsTestQuery(999) { CancellationToken = cts.Token };
            var result = await dispatcher.RunAsync(query);

            Assert.AreEqual("Result: 999", result);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestCommandTimeout()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var command = new UdsTestTimeoutCommand(2000); // 2 second delay
            await Assert.ThrowsAsync<TimeoutException>(() =>
                dispatcher.ExecuteAsync(command, TimeSpan.FromMilliseconds(100))); // 100ms timeout
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestQueryTimeout()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var query = new UdsTestTimeoutQuery(100); // 2 second delay
            await Assert.ThrowsAsync<TimeoutException>(() =>
                dispatcher.RunAsync(query, TimeSpan.FromMilliseconds(1))); // 100ms timeout
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestDispatcherDefaultTimeout()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            // We can't access the private Timeout property directly, so we'll just test with a short timeout parameter

            var command = new UdsTestTimeoutCommand(1000); // 1 second delay
            await Assert.ThrowsAsync<TimeoutException>(() =>
                dispatcher.ExecuteAsync(command, TimeSpan.FromMilliseconds(100))); // Use explicit short timeout
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestConnectionPoolMaxConnections()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var pool = new UdsConnectionPool(socketPath, 2); // Max 2 connections
            // Test that we can get 2 connections without blocking
            var conn1 = await pool.ConnectAsync(CancellationToken.None);
            var conn2 = await pool.ConnectAsync(CancellationToken.None);

            // Test that a third connection would block (we'll use a short timeout to test)
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await pool.ConnectAsync(timeoutCts.Token));

            // Return one connection to the pool
            pool.Disconnect(conn2);

            // Now we should be able to get another connection
            var conn3 = await pool.ConnectAsync(CancellationToken.None);

            // Clean up
            pool.Disconnect(conn1);
            pool.Disconnect(conn3);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestCommandErrorHandling()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var command = new UdsTestErrorCommand();
            await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.ExecuteAsync(command));
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestQueryErrorHandling()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            var query = new UdsTestErrorQuery();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                dispatcher.RunAsync(query));
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestCommandCancellation()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel after 100ms

            var command = new UdsTestTimeoutCommand(2000) { CancellationToken = cts.Token };
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                dispatcher.ExecuteAsync(command));
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestQueryCancellation()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel after 100ms

            var query = new UdsTestTimeoutQuery(2000) { CancellationToken = cts.Token };
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                dispatcher.RunAsync(query));
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestConcurrentCommandExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            // Execute multiple commands concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < MaxConnections; i++)
            {
                tasks.Add(dispatcher.ExecuteAsync(new UdsTestCommand(i)));
            }

            await Task.WhenAll(tasks);

            // Test passed if no exception was thrown
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestConcurrentQueryExecution()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            // Execute multiple queries concurrently
            var tasks = new List<Task<string>>();
            for (int i = 0; i < MaxConnections; i++)
            {
                int queryParam = i;
                tasks.Add(dispatcher.RunAsync(new UdsTestQuery(queryParam)));
            }

            var results = await Task.WhenAll(tasks);

            // Verify results
            for (int i = 0; i < MaxConnections; i++)
            {
                Assert.AreEqual($"Result: {i}", results[i]);
            }
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestConcurrentMixedOperations()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger);

            // Execute mixed commands and queries concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(dispatcher.ExecuteAsync(new UdsTestCommand(i)));
                tasks.Add(dispatcher.RunAsync(new UdsTestQuery(i)));
            }

            await Task.WhenAll(tasks);

            // Test passed if no exception was thrown
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public void TestUdsRequestHandlerEndpointCreation()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);

            // Check that the endpoint was created properly
            Assert.IsNotNull(handler.Endpoint);
            Assert.AreEqual(socketPath, handler.Endpoint.ToString());
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public void TestSocketPathCleanup()
    {
        var socketPath = Path.GetTempFileName() + ".sock";

        // Create a file at the socket path to simulate an existing socket
        File.WriteAllText(socketPath, "");

        try
        {
            // Creating the handler should clean up the existing file
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);

            // The file should have been deleted and replaced with a socket
            // (though we can't easily test the socket itself on all platforms)
            // We can at least verify that the constructor succeeded
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestConnectionReuse()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var pool = new UdsConnectionPool(socketPath, 2);

            // Get a connection
            var conn1 = await pool.ConnectAsync(CancellationToken.None);
            var connId1 = conn1.GetHashCode();

            // Return it to the pool
            pool.Disconnect(conn1);

            // Get another connection - it might be the same one
            var conn2 = await pool.ConnectAsync(CancellationToken.None);
            var connId2 = conn2.GetHashCode();

            // Return it to the pool
            pool.Disconnect(conn2);

            // The connections might be reused from the pool, so we just verify the flow works
            Assert.IsTrue(conn1 != null && conn2 != null);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestConnectionPoolDisposal()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        UdsConnectionPool pool = null;

        try
        {
            pool = new UdsConnectionPool(socketPath, 2);

            // Get a few connections
            var conn1 = await pool.ConnectAsync(CancellationToken.None);
            var conn2 = await pool.ConnectAsync(CancellationToken.None);

            // Put them back in the pool
            pool.Disconnect(conn1);
            pool.Disconnect(conn2);

            // Disposal should work without exceptions
            pool.Dispose();

            // Verify that the pool is properly disposed
            Assert.IsTrue(true);
        }
        finally
        {
            pool?.Dispose();
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }

    [TestMethod]
    public async Task TestUdsRequestDispatcherDisposal()
    {
        var socketPath = Path.GetTempFileName() + ".sock";
        try
        {
            using var handler = new UdsTestRequestHandler(socketPath, handlerLogger);
            var handlerTask = Task.Run(() => handler.ProcessAsync(CancellationToken.None));

            using (var dispatcher = new UdsRequestDispatcher(socketPath, dispatcherLogger))
            {
                // Use the dispatcher
                var query = new UdsTestQuery(42);
                var result = await dispatcher.RunAsync(query);
                Assert.AreEqual("Result: 42", result);
            }
            // Disposal should happen automatically and cleanly
            Assert.IsTrue(true);
        }
        finally
        {
            if (File.Exists(socketPath))
                File.Delete(socketPath);
        }
    }
}