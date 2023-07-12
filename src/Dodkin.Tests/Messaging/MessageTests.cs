namespace Dodkin.Tests.Messaging
{
    using System.Text.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MessageTests
    {
        private record Payload(string Data);

        [TestMethod]
        public void Serialize_SimpleMessage_ValidJson()
        {
            var payload = new Payload("test");
            var message = new Message(JsonSerializer.SerializeToUtf8Bytes(payload))
            {
                Label = "test",
            };
            var messageJson = JsonSerializer.Serialize(message);
            var messageCopy = JsonSerializer.Deserialize<Message>(messageJson);

            Assert.IsNotNull(messageCopy);
            CollectionAssert.AreEqual(message.Body.ToArray(), messageCopy.Body.ToArray());
            Assert.AreEqual(message.Label, messageCopy.Label);
        }

        [TestMethod]
        public void Equals_DefaultMessage_NotEqual()
        {
            using var message = new Message();
            using var emptyMsg = default(Message);

            Assert.IsFalse(message.Equals(emptyMsg));
        }

        [TestMethod]
        public void Serialize_EmptyMessage_IdNull()
        {
            var message = new Message();
            var json = JsonSerializer.Serialize(message);
            Assert.AreEqual("""{"MessageId":null}""", json);
        }
    }
}
