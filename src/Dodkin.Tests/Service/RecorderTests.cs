namespace Dodkin.Tests.Service;

using System.Text.Json;
using Dodkin.Service;
using Dodkin.Service.Data;
using Dodkin.Service.Recorder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RecorderTests
{
    private sealed class WorkerOptions : IOptions<ServiceOptions>
    {
        public ServiceOptions Value => new ServiceOptions
        {
            ApplicationQueue = futureQueueName.PathName,
        };
    }

    private static readonly MessageQueueName testQueueName = MessageQueueName.Parse(@".\private$\dodkin-test");
    private static readonly MessageQueueName testServiceQueueName = MessageQueueName.Parse(@".\private$\dodkin-service-test");
    private static readonly MessageQueueName futureQueueName = MessageQueueName.Parse(@".\private$\future");

    private record Payload(string Data);

    static RecorderTests()
    {
        DbTypes.Initialize();
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        MessageQueue.TryCreate(testQueueName);
        MessageQueue.TryCreate(testServiceQueueName);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        MessageQueue.TryDelete(testQueueName);
        MessageQueue.TryDelete(testServiceQueueName);
    }

    [TestMethod]
    public async Task FutureDelivery_FutureMessage_Delivered()
    {
        var cts = new CancellationTokenSource();
        var worker = new Worker(new WorkerOptions(), new MessageQueueFactory(),
            new MessageStore(
                new DbFactory(SqlClientFactory.Instance, @"Data Source=(LocalDB)\SqlLocalDB15;Initial Catalog=Dodkin;Integrated Security=SSPI;"),
                new Logger<MessageStore>(new LoggerFactory())),
            new Logger<Worker>(new LoggerFactory()));

        _ = worker.StartAsync(cts.Token);

        var payload = new Payload("test");
        var message = new Message(JsonSerializer.SerializeToUtf8Bytes(payload))
        {
            Label = "test",
            AppSpecific = (uint)DateTimeOffset.Now.AddSeconds(5).ToUnixTimeSeconds(),
            ResponseQueue = testServiceQueueName.FormatName,
        };

        using var writer = new MessageQueueWriter(futureQueueName);
        await writer.WriteAsync(message, QueueTransaction.SingleMessage);

        using var reader = new MessageQueueReader(testServiceQueueName);
        using var futureMessage = await reader.ReadAsync(MessageProperty.All);

        Assert.AreEqual(message.Label, futureMessage.Label);
        var futurePayload = JsonSerializer.Deserialize<Payload>(futureMessage.Body);
        Assert.AreEqual(payload, futurePayload);

        await worker.StopAsync(cts.Token);
        cts.Cancel();
    }
}
