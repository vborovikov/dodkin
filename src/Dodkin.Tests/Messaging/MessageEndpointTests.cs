namespace Dodkin.Tests.Messaging;

using System.ComponentModel;

[TestClass]
public class MessageEndpointTests
{
    [DataTestMethod]
    [DataRow("myqueue")]
    public void TypeConverter_InvariantString_ToMessageEndpoint(string queueName)
    {
        var endpoint = MessageEndpoint.FromName(queueName);
        var converter = TypeDescriptor.GetConverter(typeof(MessageEndpoint));
        Assert.IsNotNull(converter);
        Assert.IsTrue(converter.CanConvertFrom(typeof(string)));
        Assert.AreEqual(endpoint, converter.ConvertFromInvariantString(queueName));
    }
}