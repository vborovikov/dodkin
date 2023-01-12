namespace Dodkin;

using System.Runtime.Serialization;
using Dodkin.Interop;

[Serializable]
public class MessageQueueException : Exception
{
    protected MessageQueueException() { }

    protected MessageQueueException(string message) : base(message) { }

    internal MessageQueueException(MQ.HR errorCode) : base(errorCode.ToString())
    {
        this.ErrorCode = (uint)errorCode;
    }

    protected MessageQueueException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    protected MessageQueueException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public uint ErrorCode { get; }

    internal static void ThrowIfNotOK(MQ.HR hresult)
    {
        if (hresult != MQ.HR.OK)
            throw new MessageQueueException(hresult);
    }

    internal static void ThrowOnError(MQ.HR hresult)
    {
        if (MQ.IsFatalError(hresult))
            throw new MessageQueueException(hresult);
    }
}