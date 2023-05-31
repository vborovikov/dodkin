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
            Endpoint = new MessageEndpoint
            {
                ApplicationQueue = serviceAppQN,
                AdministrationQueue = serviceAdminQN,
            }
        };
    }

    private static readonly MessageQueueName testAppQN = MessageQueueName.Parse(@".\private$\dodkin-service-test");
    private static readonly MessageQueueName testAdminQN = MessageQueueName.Parse(@".\private$\dodkin-service-test-admin");
    private static readonly MessageQueueName serviceAppQN = MessageQueueName.Parse(@".\private$\future-test");
    private static readonly MessageQueueName serviceAdminQN = MessageQueueName.Parse(@".\private$\future-admin-test");
    private static Worker worker;

    private record Payload(string Data);

    static DeliveryTests()
    {
        DbTypes.Initialize();
    }

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        MessageQueue.TryCreate(serviceAppQN, isTransactional: true);
        MessageQueue.TryCreate(serviceAdminQN);

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

        MessageQueue.Delete(serviceAdminQN);
        MessageQueue.Delete(serviceAppQN);
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

        using var writer = new MessageQueueWriter(serviceAppQN);
        writer.Write(message, QueueTransaction.SingleMessage);

        using var reader = new MessageQueueReader(testAppQN);
        using var futureMessage = await reader.ReadAsync(MessageRecord.AllProperties);

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

        using var serviceQ = new MessageQueueWriter(serviceAppQN);
        serviceQ.Write(message, QueueTransaction.SingleMessage);

        using var adminQ = new MessageQueueReader(testAdminQN);
        using var ack = await adminQ.ReadAsync(message.Id, MessageProperty.Class);

        Assert.AreEqual(MessageClass.NackReceiveRejected, ack.Class);
    }
}
