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
        var queueName = MessageQueueName.FromFormatName(@"DIRECT=.\PRIVATE$\simple");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(@".\private$\simple", queueName.PathName);
    }
}