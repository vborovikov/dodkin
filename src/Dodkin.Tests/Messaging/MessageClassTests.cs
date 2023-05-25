namespace Dodkin.Tests.Messaging
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Net;

    [TestClass]
    public class MessageClassTests
    {
        [TestMethod]
        public void HttpStatusCode_AckReachQueue_Accepted()
        {
            Assert.AreEqual(HttpStatusCode.Accepted, MessageClass.AckReachQueue.HttpStatusCode);
        }

        [TestMethod]
        public void HttpStatusCode_AckReceive_OK()
        {
            Assert.AreEqual(HttpStatusCode.OK, MessageClass.AckReceive.HttpStatusCode);
        }

        [TestMethod]
        public void HttpStatusCode_Nack_Conflict()
        {
            Assert.AreEqual(HttpStatusCode.Conflict, MessageClass.NackAccessDenied.HttpStatusCode);
            Assert.AreEqual(HttpStatusCode.Conflict, MessageClass.NackMessagePurged.HttpStatusCode);
            Assert.AreEqual(HttpStatusCode.Conflict, MessageClass.NackReceiveRejected.HttpStatusCode);
        }
    }
}
