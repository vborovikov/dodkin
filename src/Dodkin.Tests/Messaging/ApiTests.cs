namespace Dodkin.Tests.Messaging;

using System;
using Dodkin.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ApiTests
{
    [DataTestMethod]
    [DataRow(null, 0xFFFFFFFFu)]
    [DataRow("00:00:00", 0x0u)]
    [DataRow("00:00:01", 0x3E8u)]
    [DataRow("00:05:00", 0x493E0u)]
    [DataRow("-00:00:00.0010000", 0xFFFFFFFFu)]
    [DataRow("-00:00:00.0000001", 0xFFFFFFFFu)]
    public void GetTimeout_TimeSpan_Converted(string? timeout, uint actual)
    {
        var timeSpan = string.IsNullOrWhiteSpace(timeout) ? (TimeSpan?)null : TimeSpan.Parse(timeout);
        var result = MQ.GetTimeout(timeSpan);
        Assert.AreEqual(actual, result);
    }
}
