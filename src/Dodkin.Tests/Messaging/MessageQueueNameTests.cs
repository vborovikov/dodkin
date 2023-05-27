namespace Dodkin.Tests.Messaging;

[TestClass]
public class MessageQueueNameTests
{
    [TestMethod]
    public void FromPathName_LocalPrivateQueue_DirectFormatName()
    {
        var queueName = MessageQueueName.Parse(@".\private$\simple");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(@$"DIRECT=OS:.\PRIVATE$\simple", queueName.FormatName, ignoreCase: true);
    }

    [TestMethod]
    public void FromFormatName_DirectFormatName_PathName()
    {
        var queueName = MessageQueueName.Parse(@"DIRECT=OS:.\PRIVATE$\simple");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual(@$".\PRIVATE$\simple", queueName.PathName, ignoreCase: true);
    }

    [TestMethod]
    public void FromFormatName_DirectIpx_QueueName()
    {
        var queueName = MessageQueueName.Parse(@"DIRECT=IPX: 00000012:00a0234f7500\MyQueue;JOURNAL");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual("MyQueue;JOURNAL", queueName.QueueName);
    }

    [TestMethod]
    public void FromFormatName_DirectHttpsInternet_HasMsmqPart()
    {
        var queueName = MessageQueueName.Parse("DIRECT=HTTPS://www.northwindtraders.com/msmq/MyQueue");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual("DIRECT=HTTPS://www.northwindtraders.com/msmq/MyQueue", queueName.FormatName);
        Assert.AreEqual("HTTPS://www.northwindtraders.com/msmq/MyQueue", queueName.PathName);
    }

    [TestMethod]
    public void FromFormatName_DirectSystem_HasSystemType()
    {
        var queueName = MessageQueueName.Parse(@"DIRECT=OS:Mike01\SYSTEM$;DEADLETTER");

        Assert.IsNotNull(queueName);
        Assert.AreEqual(FormatType.Direct, queueName.Format);
        Assert.AreEqual(QueueType.System, queueName.QueueType);
        Assert.AreEqual("SYSTEM$;DEADLETTER", queueName.QueueName);
    }

    [DataTestMethod]
    [DataRow("DIRECT=OS:myqueue", "DIRECT=OS:.\\myqueue")]
    [DataRow("DIRECT=TCP:10.0.0.1\\private$\\myqueue", "DIRECT=TCP:10.0.0.1\\private$\\myqueue")]
    public void FromFormatName_ValidFormatName_ReturnsMessageQueueName(string formatName, string expectedFormatName)
    {
        // Arrange

        // Act
        var result = MessageQueueName.Parse(formatName);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedFormatName, result.FormatName, ignoreCase: true);
    }

    [DataTestMethod]
    [DataRow("myqueue", ".", false)]
    [DataRow("myqueue", ".", true)]
    [DataRow("myqueue", "mycomputer", false)]
    [DataRow("myqueue", "mycomputer", true)]
    public void FromPathName_ValidPathName_ReturnsMessageQueueName(string queueName, string computerName, bool isPrivate)
    {
        // Arrange
        var pathName = $"{computerName}\\{(isPrivate ? "private$\\" : "")}{queueName}";

        // Act
        var result = MessageQueueName.Parse(pathName);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(queueName, result.QueueName);
        Assert.AreEqual(computerName, result.PathName.Split('\\')[0]);
        Assert.AreEqual(isPrivate ? QueueType.Private : QueueType.Public, result.QueueType);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("\\")]
    [DataRow("\\\\")]
    [DataRow("DIRECT=OS:")]
    public void FromFormatName_InvalidFormatName_ThrowsFormatException(string formatName)
    {
        Assert.ThrowsException<FormatException>(() => MessageQueueName.Parse(formatName));
    }

    [DataTestMethod]
    [DataRow("DIRECT=OS:myqueue", "DIRECT=OS:.\\myqueue", true)]
    [DataRow("DIRECT=TCP:10.0.0.1\\private$\\myqueue", "DIRECT=TCP:10.0.0.1\\private$\\myqueue", true)]
    [DataRow("myqueue", "DIRECT=OS:.\\private$\\myqueue", true)]
    [DataRow("private$\\myqueue", "DIRECT=OS:.\\private$\\myqueue", true)]
    [DataRow("", "", false)]
    public void TryParse_ValidInput_ReturnsExpectedResult(string input, string expectedFormatName, bool expectedResult)
    {
        // Arrange
        // Act
        var result = MessageQueueName.TryParse(input, out var messageQueueName);

        // Assert
        Assert.AreEqual(expectedResult, result);
        if (expectedResult)
        {
            Assert.IsNotNull(messageQueueName);
            Assert.AreEqual(expectedFormatName, messageQueueName!.ToString(), ignoreCase: true);
        }
        else
        {
            Assert.IsNull(messageQueueName);
        }
    }
    [DataTestMethod]
    [DataRow("DIRECT=OS:myqueue", "DIRECT=OS:.\\myqueue", QueueType.Public)]
    [DataRow("DIRECT=TCP:10.0.0.1\\private$\\myqueue", "DIRECT=TCP:10.0.0.1\\private$\\myqueue", QueueType.Private)]
    public void Parse_ValidInput_ReturnsExpectedResult(string input, string expectedFormatName, QueueType expectedQueueType)
    {
        // Arrange

        // Act
        var result = MessageQueueName.Parse(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedFormatName, result.FormatName, ignoreCase: true);
        Assert.AreEqual(expectedQueueType, result.QueueType);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    public void Parse_InvalidInput_ThrowsFormatException(string input)
    {
        // Arrange

        // Act & Assert
        Assert.ThrowsException<FormatException>(() => MessageQueueName.Parse(input));
    }
}