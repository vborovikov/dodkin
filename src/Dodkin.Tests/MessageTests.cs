namespace Dodkin.Tests
{
    using System.Text.Json;

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
    }
}
