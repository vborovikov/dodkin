namespace Dodkin;

using System.Runtime.Serialization;
using Interop;

[Serializable]
public class MessageQueueException : Exception
{
    protected MessageQueueException() { }

    protected MessageQueueException(string message) : base(message) { }

    internal MessageQueueException(MQ.HR errorCode, PropertyBag.Package? package = null)
        : base(errorCode.ToString())
    {
        this.ErrorCode = (uint)errorCode;
        package?.Dump(this.Data);
    }

    protected MessageQueueException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected MessageQueueException(string? message, Exception? innerException) : base(message, innerException) { }

    public uint ErrorCode { get; }

    internal static void ThrowIfNotOK(MQ.HR hresult, PropertyBag.Package? package = null)
    {
        if (hresult != MQ.HR.OK)
            throw new MessageQueueException(hresult, package);
    }

    internal static void ThrowOnError(MQ.HR hresult, PropertyBag.Package? package = null)
    {
        if (MQ.IsFatalError(hresult))
            throw new MessageQueueException(hresult, package);
    }
}