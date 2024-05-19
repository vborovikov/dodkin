namespace Dodkin.Tests.Dispatch;

using System;
using System.Reflection;
using Dodkin.Dispatch;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class QueueOperatorTests
{
    private static readonly MessageEndpoint endpoint = MessageEndpoint.FromName("test-dispatch");

    [ClassCleanup]
    public static void ClassCleanup()
    {
        endpoint.DeleteIfExists();
    }

    [TestMethod]
    public void ReferenceAssembly_ReferencedSameTwice_NoException()
    {
        var d = new QueueRequestDispatcher(MessageQueueName.FromName("test-queue"), endpoint, NullLogger.Instance);
        d.ReferenceAssembly(Assembly.GetExecutingAssembly());
        d.ReferenceAssembly(Assembly.GetExecutingAssembly());

        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ReferenceAssembly_ReferencedDifferent_Exception()
    {
        var d = new QueueRequestDispatcher(MessageQueueName.FromName("test-queue"), endpoint, NullLogger.Instance);
        d.ReferenceAssembly(Assembly.GetExecutingAssembly());
        Assert.ThrowsException<InvalidOperationException>(() => d.ReferenceAssembly(typeof(ServiceStatusQuery).Assembly));
    }
}
