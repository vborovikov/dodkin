namespace Dodkin.Tests;

using Dodkin.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

[TestClass]
public class MessageQueueTests
{
    private const string TestQueuePathName = @".\private$\dodkin-test";
    private static MessageQueueName testQueueName = MessageQueueName.FromPathName(TestQueuePathName);

    private record Payload(string Data);

    [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        testQueueName = MessageQueue.TryCreate(testQueueName, maxStorageSize: 1024);
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        MessageQueue.TryDelete(testQueueName);
    }

    [TestMethod]
    public void FormatName_NonExistingQueue_Exception()
    {
		try
		{
			var factory = new MessageQueueFactory();
			var reader = factory.CreateReader(MessageQueueName.FromPathName(@".\private$\doesnotexist")) as MessageQueueReader;
			Assert.IsNotNull(reader);
			_ = reader.FormatName;
		}
		catch (MessageQueueException x)
		{
			Assert.AreEqual((uint)MQ.HR.ERROR_QUEUE_NOT_ACTIVE, x.ErrorCode);
        }
		catch
		{
			Assert.Fail();
		}
    }

    [TestMethod]
    public void SendReceive_Properties_Preserved()
    {
        using var queueWriter = new MessageQueueWriter(testQueueName);
        using var queueReader = new MessageQueueReader(testQueueName);

        var msgSent = new Message(JsonSerializer.SerializeToUtf8Bytes(new Payload("123")))
        {
            Label = "Hello",
        };

        queueWriter.Write(msgSent);
        var msgReceived = queueReader.Read(
            MessageProperty.MessageId | MessageProperty.Label | MessageProperty.Body);

        Assert.IsNotNull(msgReceived);
        Assert.AreEqual(msgSent.Id, msgReceived.Id);
        Assert.AreEqual("Hello", msgReceived.Label);

        var payload = JsonSerializer.Deserialize<Payload>(msgReceived.Body);
        Assert.IsNotNull(payload);
        Assert.AreEqual("123", payload.Data);
    }

    [TestMethod]
    public async Task ReceiveCancel_CorrectState_Received()
    {
        using var queueWriter = new MessageQueueWriter(testQueueName);
        using var queueReader = new MessageQueueReader(testQueueName);

        using var cancelSource = new CancellationTokenSource();
        _ = queueReader.ReadAsync(MessageProperty.MessageId | MessageProperty.Label | MessageProperty.Body,
            TimeSpan.FromMinutes(1), cancelSource.Token);
        await Task.Delay(1000);
        cancelSource.Cancel();

        var msgSent = new Message(JsonSerializer.SerializeToUtf8Bytes(new Payload("123")))
        {
            Label = "Hello",
        };

        await queueWriter.WriteAsync(msgSent);
        var msgReceived = await queueReader.ReadAsync(
            MessageProperty.MessageId | MessageProperty.Label | MessageProperty.Body);

        Assert.IsNotNull(msgReceived);
        Assert.AreEqual(msgSent.Id, msgReceived.Id);
        Assert.AreEqual("Hello", msgReceived.Label);

        var payload = JsonSerializer.Deserialize<Payload>(msgReceived.Body);
        Assert.IsNotNull(payload);
        Assert.AreEqual("123", payload.Data);
    }

    [TestMethod]
    public void GetMachineInfo_HaveQueues_NonEmpty()
    {
        var machineInfo = MessageQueue.GetMachineInfo();

        Assert.IsNotNull(machineInfo.ActiveQueueFormatNames);
        if (machineInfo.ActiveQueueFormatNames.Any())
        {
            CollectionAssert.AllItemsAreNotNull(machineInfo.ActiveQueueFormatNames);
            CollectionAssert.AllItemsAreUnique(machineInfo.ActiveQueueFormatNames);
        }

        if (machineInfo.PrivateQueuePathNames.Any())
        {
            CollectionAssert.AllItemsAreNotNull(machineInfo.PrivateQueuePathNames);
            CollectionAssert.AllItemsAreUnique(machineInfo.PrivateQueuePathNames);
        }

        Assert.IsTrue(machineInfo.IsConnected);
        Assert.IsNotNull(machineInfo.Type);
    }

    [TestMethod]
    public async Task GetQueueInfo_PrivateQueue_HasInfo()
    {
        var machineInfo = MessageQueue.GetMachineInfo();
        var queuePath = machineInfo.PrivateQueuePathNames.FirstOrDefault();
        Assert.IsNotNull(queuePath);

        using var queueWriter = new MessageQueueWriter(testQueueName);
        var msg = new Message(JsonSerializer.SerializeToUtf8Bytes(new Payload("123")))
        {
            Label = "Hello",
        };
        await queueWriter.WriteAsync(msg);

        var queueInfo = MessageQueue.GetQueueInfo(testQueueName);
        Assert.IsNotNull(queueInfo.FormatName);
        Assert.AreNotEqual(0, queueInfo.MessageCount);

        MessageQueue.Purge(testQueueName);
    }
}