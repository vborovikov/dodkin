namespace Dodkin.Tests.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tools;

[TestClass]
public class MessageIdTests
{
    [TestMethod]
    public void Equality_DefaultGiven_Tested()
    {
        var messageId = new MessageId(NewMessageIdBuffer());

        EqualityTests.TestEqualObjects(default, new MessageId());
        EqualityTests.TestUnequalObjects(default, messageId);
        EqualityTests.TestAgainstNull(messageId);
    }

    [TestMethod]
    public void Equality_TwoDifferentIds_Tested()
    {
        var msgId1 = new MessageId(NewMessageIdBuffer());
        var msgId2 = new MessageId(NewMessageIdBuffer());

        EqualityTests.TestUnequalObjects(msgId1, msgId2);
    }

    [TestMethod]
    public void Parse_RandomParsed_Equal()
    {
        var msgId = new MessageId(NewMessageIdBuffer());
        var msgIdParsed = MessageId.Parse(msgId.ToString());

        EqualityTests.TestEqualObjects(msgId, msgIdParsed);
    }

    private static byte[] NewMessageIdBuffer()
    {
        var guid = Guid.NewGuid();
        var id = Random.Shared.Next();

        var buffer = new byte[20];
        guid.TryWriteBytes(buffer);
        BitConverter.TryWriteBytes(buffer.AsSpan(16), id);

        return buffer;
    }
}
