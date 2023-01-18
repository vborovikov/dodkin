namespace Dodkin.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MessageQueueNameTests
{
    [TestMethod]
    public void FromPathName_LocalPrivateQueue_DirectFormatName()
    {
        var queueName = MessageQueueName.FromPathName(@".\private$\simple");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(@$"DIRECT=OS:{Environment.MachineName}\PRIVATE$\simple", queueName.FormatName);
    }

    [TestMethod]
    public void FromFormatName_DirectFormatName_PathName()
    {
        var queueName = MessageQueueName.FromFormatName(@"DIRECT=OS:.\PRIVATE$\simple");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual(@$"{Environment.MachineName}\PRIVATE$\simple", queueName.PathName);
    }

    [TestMethod]
    public void FromFormatName_DirectIpx_QueueName()
    {
        var queueName = MessageQueueName.FromFormatName(@"DIRECT=IPX: 00000012:00a0234f7500\MyQueue;JOURNAL");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual("MyQueue;JOURNAL", queueName.QueueName);
    }

    [TestMethod]
    public void FromFormatName_DirectHttpsInternet_HasMsmqPart()
    {
        var queueName = MessageQueueName.FromFormatName("DIRECT=HTTPS://www.northwindtraders.com/msmq/MyQueue");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual("DIRECT=HTTPS://www.northwindtraders.com/msmq/MyQueue", queueName.FormatName);
        Assert.AreEqual("HTTPS://www.northwindtraders.com/msmq/MyQueue", queueName.PathName);
    }

    [TestMethod]
    public void FromFormatName_DirectSystem_HasSystemType()
    {
        var queueName = MessageQueueName.FromFormatName(@"DIRECT=OS:Mike01\SYSTEM$;DEADLETTER");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual(QueueType.System, queueName.QueueType);
        Assert.AreEqual("SYSTEM$;DEADLETTER", queueName.QueueName);
    }
}