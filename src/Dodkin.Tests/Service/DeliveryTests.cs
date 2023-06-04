namespace Dodkin.Tests.Service;

using System.Text.Json;
using Dodkin.Service;
using Dodkin.Service.Data;
using Dodkin.Service.Delivery;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DeliveryTests
{
    private sealed class WorkerOptions : IOptions<ServiceOptions>
    {
        public ServiceOptions Value => new ServiceOptions
        {
            Endpoint = serviceEndpoint
        };
    }

    private static readonly MessageEndpoint serviceEndpoint = MessageEndpoint.FromName("future-test");
    private static readonly MessageQueueName testAppQN = MessageQueueName.Parse(@".\private$\dodkin-service-test");
    private static readonly MessageQueueName testAdminQN = MessageQueueName.Parse(@".\private$\dodkin-service-test-admin");
    private static Worker worker;

    private record Payload(string Data);

    static DeliveryTests()
    {
        DbTypes.Initialize();
    }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        worker = new Worker(new WorkerOptions(), new MessageQueueFactory(),
            new MessageStore(
                SqlClientFactory.Instance.CreateDataSource(@"Data Source=(LocalDB)\SqlLocalDB15;Initial Catalog=Dodkin;Integrated Security=SSPI;"),
                new Logger<MessageStore>(new LoggerFactory())),
            new Logger<Worker>(new LoggerFactory()));

        await worker.StartAsync(default);

        MessageQueue.TryCreate(testAppQN, isTransactional: true);
        MessageQueue.TryCreate(testAdminQN);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        MessageQueue.Delete(testAdminQN);
        MessageQueue.Delete(testAppQN);

        await worker.StopAsync(default);

        MessageQueue.Delete(serviceEndpoint.DeadLetterQueue);
        MessageQueue.Delete(serviceEndpoint.AdministrationQueue);
        MessageQueue.Delete(serviceEndpoint.ApplicationQueue);
    }

    [TestMethod]
    public async Task ExecuteAsync_FutureMessage_Delivered()
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

        using var writer = new MessageQueueWriter(serviceEndpoint.ApplicationQueue);
        writer.Write(message, QueueTransaction.SingleMessage);

        using var reader = new MessageQueueReader(testAppQN);
        using var futureMessage = await reader.ReadAsync(MessageProperty.All);

        Assert.AreEqual(message.Label, futureMessage.Label);
        var futurePayload = JsonSerializer.Deserialize<Payload>(futureMessage.Body);
        Assert.AreEqual(payload, futurePayload);
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidMessage_Rejected()
    {
        var payload = new Payload("test");
        using var message = new Message(JsonSerializer.SerializeToUtf8Bytes(payload))
        {
            Label = "invalid",
            AppSpecific = 0,
            ResponseQueue = testAppQN,
            AdministrationQueue = testAdminQN,
            Acknowledgment = MessageAcknowledgment.FullReceive,
            TimeToBeReceived = TimeSpan.FromSeconds(5),
        };

        using var serviceQ = new MessageQueueWriter(serviceEndpoint.ApplicationQueue);
        serviceQ.Write(message, QueueTransaction.SingleMessage);

        using var adminQ = new MessageQueueReader(testAdminQN);
        using var ack = await adminQ.ReadAsync(message.Id, MessageProperty.Class);

        Assert.AreEqual(MessageClass.NackReceiveRejected, ack.Class);
    }
}
