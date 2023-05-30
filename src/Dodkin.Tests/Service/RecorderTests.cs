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
            Endpoint = new MessageEndpoint
            {
                ApplicationQueue = appQN,
                AdministrationQueue = adminQN,
            }
        };
    }

    private static readonly MessageQueueName testAppQN = MessageQueueName.Parse(@".\private$\dodkin-service-test");
    private static readonly MessageQueueName testAdminQN = MessageQueueName.Parse(@".\private$\dodkin-service-test-admin");
    private static readonly MessageQueueName appQN = MessageQueueName.Parse(@".\private$\future-test");
    private static readonly MessageQueueName adminQN = MessageQueueName.Parse(@".\private$\future-admin-test");
    private static Worker worker;

    private record Payload(string Data);

    static RecorderTests()
    {
        DbTypes.Initialize();
    }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        MessageQueue.TryCreate(appQN, isTransactional: true);
        MessageQueue.TryCreate(adminQN);

        worker = new Worker(new WorkerOptions(), new MessageQueueFactory(),
            new MessageStore(
                new DbFactory(SqlClientFactory.Instance, @"Data Source=(LocalDB)\SqlLocalDB15;Initial Catalog=Dodkin;Integrated Security=SSPI;"),
                new Logger<MessageStore>(new LoggerFactory())),
            new Logger<Worker>(new LoggerFactory()));

        await worker.StartAsync(default);

        MessageQueue.TryCreate(testAppQN);
        MessageQueue.TryCreate(testAdminQN);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        MessageQueue.Delete(testAdminQN);
        MessageQueue.Delete(testAppQN);

        await worker.StopAsync(default);

        MessageQueue.Delete(adminQN);
        MessageQueue.Delete(appQN);
    }

    [TestMethod]
    public async Task FutureDelivery_FutureMessage_Delivered()
    {
        var payload = new Payload("test");
        var message = new Message(JsonSerializer.SerializeToUtf8Bytes(payload))
        {
            Label = "test",
            AppSpecific = (uint)DateTimeOffset.Now.AddSeconds(5).ToUnixTimeSeconds(),
            ResponseQueue = testAppQN,
            AdministrationQueue = testAdminQN,
            Acknowledgment = MessageAcknowledgment.FullReceive,
            TimeToReachQueue = TimeSpan.FromSeconds(5),
            TimeToBeReceived = TimeSpan.FromSeconds(5),
        };

        using var writer = new MessageQueueWriter(appQN);
        writer.Write(message, QueueTransaction.SingleMessage);

        using var reader = new MessageQueueReader(testAppQN);
        using var futureMessage = await reader.ReadAsync(MessageRecord.AllProperties);

        Assert.AreEqual(message.Label, futureMessage.Label);
        var futurePayload = JsonSerializer.Deserialize<Payload>(futureMessage.Body);
        Assert.AreEqual(payload, futurePayload);
    }
}
